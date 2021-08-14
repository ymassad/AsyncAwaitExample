using System;
using System.Collections.Concurrent;

namespace TransactionWebService1.Controllers
{
    public sealed class StatePerTransaction<TStateObject>
    {
        private readonly Func<TStateObject> createNew;

        private readonly ConcurrentDictionary<Guid, TStateObject> dictionary =
            new ConcurrentDictionary<Guid, TStateObject>();

        public StatePerTransaction(Func<TStateObject> createNew)
        {
            this.createNew = createNew;
        }

        public void InitializeState(Guid transactionId)
        {
            var stateObject = createNew();
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