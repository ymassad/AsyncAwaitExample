using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace TransactionWebService.Controllers
{
    [ApiController]
    public class AsyncAwaitController : ControllerBase
    {

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
            var collection = GetStateObjectOrThrow(transactionId).Collection;
            
            collection.Add(item);
        }

        [HttpPost]
        [Route("AsyncAwait/EndTransaction")]
        public void EndTransaction(Guid transactionId)
        {
            var collection = GetStateObjectOrThrow(transactionId).Collection;

            collection.CompleteAdding();
        }

        private async Task HandleTransaction(Guid transactionId)
        {
            InitializeState(transactionId);

            try
            {
                var connectionString = "Data Source=.\\SQLEXPRESS;Initial Catalog=AsyncExampleDb;Integrated Security=SSPI;";

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
                RemoveStateObject(transactionId);
            }
        }


        private async Task<(bool ended, DataPointDTO? obj)> WaitForNextItem(Guid transactionId)
        {
            var collection = GetStateObjectOrThrow(transactionId).Collection;

            if (!await collection.OutputAvailableAsync())
            {
                return (true, null);
            }

            return (false, collection.Take());
        }

        class StateObject
        {
            public AsyncCollection<DataPointDTO> Collection { get; }

            public StateObject(AsyncCollection<DataPointDTO> collection)
            {
                Collection = collection;
            }
        }
        
    }

    public sealed class StatePerTransaction<TStateObject>
    {
        private readonly Func<TStateObject> createNew;

        private ConcurrentDictionary<Guid, TStateObject> dictionary =
            new ConcurrentDictionary<Guid, TStateObject>();

        public StatePerTransaction(Func<TStateObject> createNew)
        {
            this.createNew = createNew;
        }


        private void InitializeState(Guid transactionId)
        {
            var stateObject = createNew();
            if (!dictionary.TryAdd(transactionId, stateObject))
                throw new Exception("Could not add state object");
        }


        private TStateObject GetStateObjectOrThrow(Guid transactionId)
        {
            if (!dictionary.TryGetValue(transactionId, out var stateObject))
                throw new Exception("Unable to find state object for specified transaction id");

            return stateObject;
        }

        private void RemoveStateObject(Guid transactionId)
        {
            dictionary.TryRemove(transactionId, out _);
        }

    }





}
