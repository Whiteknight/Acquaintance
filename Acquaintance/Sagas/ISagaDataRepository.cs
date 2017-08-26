namespace Acquaintance.Sagas
{
    public interface ISagaDataRepository<TState, TKey>
    {
        bool SaveNew(TKey key, ISagaContext<TState, TKey> context);
        bool SaveExisting(TKey key, ISagaContext<TState, TKey> context);
        ISagaContext<TState, TKey> Get(TKey key);
        void RemoveState(TKey key);
    }
}