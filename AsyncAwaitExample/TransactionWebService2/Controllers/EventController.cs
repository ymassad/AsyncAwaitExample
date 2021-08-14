using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Timer = System.Timers.Timer;

namespace TransactionWebService2.Controllers
{
    [ApiController]
    public class EventController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly StatePerTransaction<StateObject> statePerTransaction;

        public EventController(IConfiguration configuration, StatePerTransaction<StateObject> statePerTransaction)
        {
            this.configuration = configuration;
            this.statePerTransaction = statePerTransaction;
        }

        [HttpPost]
        [Route("Event/StartTransaction")]
        public Guid StartTransaction()
        {
            var transactionId = Guid.NewGuid();

            var connectionString = configuration.GetConnectionString("ConnectionString");

            var context = new DatabaseContext(connectionString);

            context.Database.EnsureCreatedAsync();

            var reset = TimeoutManager.RunActionAfter(Program.TimeoutSpan, () =>
            {
                try
                {
                    context.Dispose();
                }
                finally
                {
                    statePerTransaction.RemoveStateObject(transactionId);
                }
            });

            statePerTransaction.InitializeState(transactionId, new StateObject(context, reset));

            return transactionId;
        }

        [HttpPost]
        [Route("Event/Add")]
        public void Add(Guid transactionId, [FromBody] DataPointDTO item)
        {
            var stateObject = statePerTransaction.GetStateObjectOrThrow(transactionId);
            var context = stateObject.DatabaseContext;

            try
            {
                var dataPointEntity = new DataPoint()
                {
                    Value = item.Value
                };

                context.DataPoints.Add(dataPointEntity);
            }
            catch
            {
                try
                {
                    context.Dispose();
                }
                finally
                {
                    statePerTransaction.RemoveStateObject(transactionId);
                }

                throw;
            }

            stateObject.RestTimeout(cancel: false);
        }

        [HttpPost]
        [Route("Event/EndTransaction")]
        public void EndTransaction(Guid transactionId)
        {
            var stateObject = statePerTransaction.GetStateObjectOrThrow(transactionId);

            try
            {
                var context = stateObject.DatabaseContext;

                try
                {
                    context.SaveChanges();
                }
                finally
                {
                    context.Dispose();
                }
            }
            finally
            {
                statePerTransaction.RemoveStateObject(transactionId);
            }

            stateObject.RestTimeout(cancel: true);
        }

        public class StateObject
        {
            public DatabaseContext DatabaseContext { get; }

            public TimeoutManager.Reset RestTimeout { get; }

            public StateObject(DatabaseContext databaseContext, TimeoutManager.Reset restTimeout)
            {
                DatabaseContext = databaseContext;
                RestTimeout = restTimeout;
            }
        }

        public class TimeoutManager
        {
            public delegate bool Reset(bool cancel);

            public static Reset RunActionAfter(TimeSpan after, Action action)
            {
                var timer = new Timer(after.TotalMilliseconds);

                bool cannotResetAnymore = false;

                bool reset = false;

                timer.Elapsed += (o, e) =>
                {
                    lock (timer)
                    {
                        if (reset)
                        {
                            reset = false;
                            return;
                        }

                        cannotResetAnymore = true;
                    }

                    action();
                };

                timer.AutoReset = false;

                timer.Start();

                return (cancel) =>
                {
                    lock (timer)
                    {
                        if (reset)
                            return true;

                        if (cannotResetAnymore)
                            return false;

                        reset = true;

                        timer.Stop();

                        if (!cancel)
                        {
                            timer.Start();
                        }

                        return true;
                    }
                };

            }
        }
    }
}
