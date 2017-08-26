namespace Acquaintance.Sagas
{
    public class SagaContext<TState, TKey> : ISagaContext<TState, TKey>
    {
        public bool IsAborted { get; private set; }
        public bool IsCompleted { get; private set; }

        public void Abort()
        {
            IsAborted = true;
        }

        public void Complete()
        {
            IsCompleted = true;
        }

        public TKey Key { get; set; }
        public TState State { get; set; }
    }
}