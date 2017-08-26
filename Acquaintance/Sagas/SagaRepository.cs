using System.Collections.Concurrent;

namespace Acquaintance.Sagas
{
    // TODO: Need a way to clean out old/incomplete contexts
    public class SagaRepository<TState, TKey> : ISagaDataRepository<TState, TKey>
    {
        private readonly ConcurrentDictionary<TKey, ISagaContext<TState, TKey>> _store;

        public SagaRepository()
        {
            _store = new ConcurrentDictionary<TKey, ISagaContext<TState, TKey>>();
        }

        public bool SaveNew(TKey key, ISagaContext<TState, TKey> context)
        {
            return _store.TryAdd(key, context);
        }

        public bool SaveExisting(TKey key, ISagaContext<TState, TKey> context)
        {
            return true;
        }

        public ISagaContext<TState, TKey> Get(TKey key)
        {
            return _store.TryGetValue(key, out ISagaContext<TState, TKey> context) ? context : null;
        }

        public void RemoveState(TKey key)
        {
            _store.TryRemove(key, out ISagaContext<TState, TKey> context);
        }
    }
}