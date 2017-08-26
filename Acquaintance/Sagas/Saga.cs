using System;
using Acquaintance.Utility;

namespace Acquaintance.Sagas
{
    public class Saga<TState, TKey> : IDisposable
    {
        private IPubSubBus MessageBus { get; }
        private readonly ISagaDataRepository<TState, TKey> _repository;
        private readonly int _threadId;
        private readonly DisposableCollection _tokens;

        private Action<IPublishable, TState> OnComplete { get; set; }

        public Saga(IPubSubBus messageBus, ISagaDataRepository<TState, TKey> repository, int threadId)
        {
            MessageBus = messageBus;
            _repository = repository;
            _threadId = threadId;
            _tokens = new DisposableCollection();
        }

        public void StartWith<TPayload>(string topic, Func<TPayload, TKey> getKey, Func<TPayload, TState> createState, Action<ISagaContext<TState, TKey>> onReceive)
        {
            var token = MessageBus.Subscribe<TPayload>(b => b
                .WithTopic(topic)
                .Invoke(p => StartSagaInstance(p, getKey, createState, onReceive))
                .OnThread(_threadId));
            _tokens.Add(token);
        }

        private void StartSagaInstance<TPayload>(TPayload payload, Func<TPayload, TKey> getKey, Func<TPayload, TState> createState, Action<ISagaContext<TState, TKey>> onReceive)
        {
            var key = getKey(payload);
            var state = createState(payload);
            if (state == null)
                return;
            var context = new SagaContext<TState, TKey>()
            {
                State = state,
                Key = key
            };
            bool ok = _repository.SaveNew(key, context);
            if (!ok)
                return;

            onReceive?.Invoke(context);
            CheckStateCompletion(context, key, state);
        }

        private void CheckStateCompletion(ISagaContext<TState, TKey> context, TKey key, TState state)
        {
            if (context.IsAborted)
            {
                _repository.RemoveState(key);
                return;
            }
            if (context.IsCompleted)
            {
                _repository.RemoveState(key);
                OnComplete?.Invoke(MessageBus, state);
                return;
            }
            _repository.SaveExisting(key, context);
        }

        public void ContinueWith<TPayload>(string topic, Func<TPayload, TKey> getKey, Action<ISagaContext<TState, TKey>, TPayload> onReceive)
        {
            var token = MessageBus.Subscribe<TPayload>(b => b
                .WithTopic(topic)
                .Invoke(p => ContinueSagaInstance(p, getKey, onReceive))
                .OnThread(_threadId));
            _tokens.Add(token);
        }

        private void ContinueSagaInstance<TPayload>(TPayload payload, Func<TPayload, TKey> getKey, Action<ISagaContext<TState, TKey>, TPayload> onReceive)
        {
            var key = getKey(payload);
            var context = _repository.Get(key);
            onReceive?.Invoke(context, payload);
            CheckStateCompletion(context, key, context.State);
        }

        public void WhenCompleted(Action<IPublishable, TState> onComplete)
        {
            OnComplete = onComplete;
        }

        public void Dispose()
        {
            _tokens?.Dispose();
        }
    }
}
