using System;

namespace Acquaintance.Sagas
{
    public interface ISagaBuilder<TState, TKey>
    {
        ISagaBuilder<TState, TKey> StartWith<TPayload>(string topic, Func<TPayload, TKey> getKey, Func<TPayload, TState> createState, Action<ISagaContext<TState, TKey>> onReceive);
        ISagaBuilder<TState, TKey> ContinueWith<TPayload>(string topic, Func<TPayload, TKey> getKey, Action<ISagaContext<TState, TKey>, TPayload> onReceive);
        void WhenCompleted(Action<IPublishable, TState> onComplete);
    }
}