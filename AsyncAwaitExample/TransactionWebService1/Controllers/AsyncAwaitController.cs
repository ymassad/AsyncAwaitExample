using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Nito.AsyncEx;

namespace TransactionWebService1.Controllers
{
    [ApiController]
    public class AsyncAwaitController : ControllerBase
    {
        private readonly IConfiguration configuration;

        public AsyncAwaitController(IConfiguration configuration)
        {
            this.configuration = configuration;
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
            var collection = statePerTransaction.GetStateObjectOrThrow(transactionId).Collection;
            
            collection.Add(item);
        }

        [HttpPost]
        [Route("AsyncAwait/EndTransaction")]
        public void EndTransaction(Guid transactionId)
        {
            var collection = statePerTransaction.GetStateObjectOrThrow(transactionId).Collection;

            collection.CompleteAdding();
        }

        private async Task HandleTransaction(Guid transactionId)
        {
            statePerTransaction.InitializeState(transactionId);

            try
            {
                var connectionString = configuration.GetConnectionString("ConnectionString");

                await using var context = new DatabaseContext(connectionString);

                await context.Database.EnsureCreatedAsync();

                while (true)
                {
                    var item = await WaitForNextItem(transactionId).ConfigureAwait(false);

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

        private static StatePerTransaction<StateObject> statePerTransaction = new StatePerTransaction<StateObject>(
            () => new StateObject(new AsyncCollection<DataPointDTO>()));

        class StateObject
        {
            public AsyncCollection<DataPointDTO> Collection { get; }

            public StateObject(AsyncCollection<DataPointDTO> collection)
            {
                Collection = collection;
            }
        }
    }
}
