using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Nito.AsyncEx;

namespace TransactionWebService1.Controllers
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

            statePerTransaction.InitializeState(transactionId, new StateObject(context));

            return transactionId;
        }

        [HttpPost]
        [Route("Event/Add")]
        public void Add(Guid transactionId, [FromBody] DataPointDTO item)
        {
            var stateObject = statePerTransaction.GetStateObjectOrThrow(transactionId);

            try
            {
                var context = stateObject.DatabaseContext;

                var dataPointEntity = new DataPoint()
                {
                    Value = item.Value
                };

                context.DataPoints.Add(dataPointEntity);
            }
            catch
            {
                statePerTransaction.RemoveStateObject(transactionId);
                throw;
            }
        }

        [HttpPost]
        [Route("Event/EndTransaction")]
        public void EndTransaction(Guid transactionId)
        {
            var stateObject = statePerTransaction.GetStateObjectOrThrow(transactionId);

            try
            {
                var context = stateObject.DatabaseContext;

                context.SaveChanges();
            }
            finally
            {
                statePerTransaction.RemoveStateObject(transactionId);
            }
        }

        public class StateObject
        {
            public DatabaseContext DatabaseContext { get; set; }

            public StateObject(DatabaseContext databaseContext)
            {
                DatabaseContext = databaseContext;
            }
        }
    }
}
