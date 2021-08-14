using System;
using System.Collections.Concurrent;

namespace TransactionWebService1.Controllers
{
    public sealed class StatePerTransaction<TStateObject>
    {
        private readonly ConcurrentDictionary<Guid, TStateObject> dictionary =
            new ConcurrentDictionary<Guid, TStateObject>();

        public StatePerTransaction()
        {
        }

        public void InitializeState(Guid transactionId, TStateObject stateObject)
        {
            if (!dictionary.TryAdd(transactionId, stateObject))
                throw new Exception("Could not add state object");
        }

        public TStateObject GetStateObjectOrThrow(Guid transactionId)
        {
            if (!dictionary.TryGetValue(transactionId, out var stateObject))
                throw new Exception("Unable to find state object for specified transaction id");

            return stateObject;
        }

        public void RemoveStateObject(Guid transactionId)
        {
            dictionary.TryRemove(transactionId, out _);
        }
    }
}