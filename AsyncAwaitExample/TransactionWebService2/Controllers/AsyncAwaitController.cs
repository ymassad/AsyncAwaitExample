using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Nito.AsyncEx;

namespace TransactionWebService2.Controllers
{
    [ApiController]
    public class AsyncAwaitController : ControllerBase
    {
        private readonly IConfiguration configuration;

        private readonly StatePerTransaction<StateObject> statePerTransaction;

        public AsyncAwaitController(IConfiguration configuration, StatePerTransaction<StateObject> statePerTransaction)
        {
            this.configuration = configuration;
            this.statePerTransaction = statePerTransaction;
        }

        [HttpPost]
        [Route("AsyncAwait/StartTransaction")]
        public Guid StartTransaction()
        {
            var transactionId = Guid.NewGuid();
            
            HandleTransaction(transactionId);

            return transactionId;
        }

        [HttpPost]
        [Route("AsyncAwait/Add")]
        public void Add(Guid transactionId, [FromBody] DataPointDTO item)
        {
            statePerTransaction
                .GetStateObjectOrThrow(transactionId)
                .Collection
                .Add(item);
        }

        [HttpPost]
        [Route("AsyncAwait/EndTransaction")]
        public void EndTransaction(Guid transactionId)
        {
            statePerTransaction
                .GetStateObjectOrThrow(transactionId)
                .Collection
                .CompleteAdding();
        }

        private async Task HandleTransaction(Guid transactionId)
        {
            statePerTransaction.InitializeState(transactionId, new StateObject(new AsyncCollection<DataPointDTO>()));

            try
            {
                var connectionString = configuration.GetConnectionString("ConnectionString");

                await using var context = new DatabaseContext(connectionString);

                await context.Database.EnsureCreatedAsync();

                while (true)
                {
                    var item = await WaitForNextItem(transactionId).TimeoutAfter(Program.TimeoutSpan).ConfigureAwait(false);

                    if (item.ended)
                    {
                        //Transaction complete
                        await context.SaveChangesAsync();

                        return;
                    }
                    else
                    {
                        var dto = item.obj ?? throw new Exception("Unexpected: obj is null");

                        var dataPointEntity = new DataPoint()
                        {
                            Value = dto.Value
                        };
                    
                        context.DataPoints.Add(dataPointEntity);
                    }
                }
            }
            finally
            {
                statePerTransaction.RemoveStateObject(transactionId);
            }
        }

        private async Task<(bool ended, DataPointDTO? obj)> WaitForNextItem(Guid transactionId)
        {
            var collection = statePerTransaction.GetStateObjectOrThrow(transactionId).Collection;

            if (!await collection.OutputAvailableAsync())
            {
                return (true, null);
            }

            return (false, collection.Take());
        }

        public class StateObject
        {
            public AsyncCollection<DataPointDTO> Collection { get; }

            public StateObject(AsyncCollection<DataPointDTO> collection)
            {
                Collection = collection;
            }
        }
    }
}
