namespace Acquaintance.Sagas
{
    public interface ISagaContext<TState, TKey>
    {
        TKey Key { get; set; }
        TState State { get; set; }
        void Complete();
        void Abort();
        bool IsAborted { get; }

        bool IsCompleted { get; }
    }
}