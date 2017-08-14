using Acquaintance.PubSub;
using Acquaintance.RequestResponse;
using Acquaintance.ScatterGather;
using Acquaintance.Threading;
using System;
using Acquaintance.Utility;

namespace Acquaintance
{
    /// <summary>
    /// SubscriptionCollection holds a collection of subscription tokens so that they can all be
    /// disposed at once.
    /// </summary>
    public sealed class SubscriptionCollection : IPubSubBus, IReqResBus, IScatterGatherBus, IDisposable
    {
        private readonly IMessageBus _messageBus;
        private readonly DisposableCollection _subscriptions;

        public SubscriptionCollection(IMessageBus messageBus)
        {
            _messageBus = messageBus;
            _subscriptions = new DisposableCollection();
        }

        public IThreadPool ThreadPool => new DisposableThreadPool(_messageBus.ThreadPool, _subscriptions);

        public IEnvelopeFactory EnvelopeFactory => _messageBus.EnvelopeFactory;

        public IDisposable Subscribe<TPayload>(string topic, ISubscription<TPayload> subscription)
        {
            var token = _messageBus.Subscribe(topic, subscription);
            _subscriptions.Add(token);
            return token;
        }

        public IDisposable Listen<TRequest, TResponse>(string topic, IListener<TRequest, TResponse> listener)
        {
            var token = _messageBus.Listen(topic, listener);
            _subscriptions.Add(token);
            return token;
        }

        public IDisposable Participate<TRequest, TResponse>(string topic, IParticipant<TRequest, TResponse> participant)
        {
            var token = _messageBus.Participate(topic, participant);
            _subscriptions.Add(token);
            return token;
        }

        public IRequest<TResponse> RequestEnvelope<TRequest, TResponse>(Envelope<TRequest> envelope)
        {
            return _messageBus.RequestEnvelope<TRequest, TResponse>(envelope);
        }

        public IScatter<TResponse> Scatter<TRequest, TResponse>(string topic, TRequest request)
        {
            return _messageBus.Scatter<TRequest, TResponse>(topic, request);
        }

        public void PublishEnvelope<TPayload>(Envelope<TPayload> envelope)
        {
            _messageBus.PublishEnvelope(envelope);
        }

        public void Clear()
        {
            _subscriptions.Dispose();
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }

        private class DisposableThreadPool : IThreadPool
        {
            private readonly IThreadPool _inner;
            private readonly DisposableCollection _tokens;

            public DisposableThreadPool(IThreadPool inner, DisposableCollection tokens)
            {
                _inner = inner;
                _tokens = tokens;
            }

            public void Dispose()
            {
            }

            public int NumberOfRunningFreeWorkers => _inner.NumberOfRunningFreeWorkers;

            public ThreadReport GetThreadReport()
            {
                return _inner.GetThreadReport();
            }

            public ThreadToken StartDedicatedWorker()
            {
                var token = _inner.StartDedicatedWorker();
                _tokens.Add(token);
                return token;
            }

            public void StopDedicatedWorker(int threadId)
            {
                _inner.StopDedicatedWorker(threadId);
            }

            public IActionDispatcher GetThreadDispatcher(int threadId, bool allowAutoCreate)
            {
                return _inner.GetThreadDispatcher(threadId, allowAutoCreate);
            }

            public IActionDispatcher GetFreeWorkerThreadDispatcher()
            {
                return _inner.GetFreeWorkerThreadDispatcher();
            }

            public IActionDispatcher GetThreadPoolActionDispatcher()
            {
                return _inner.GetThreadPoolActionDispatcher();
            }

            public IActionDispatcher GetAnyThreadDispatcher()
            {
                return _inner.GetAnyThreadDispatcher();
            }

            public IActionDispatcher GetCurrentThreadDispatcher()
            {
                return _inner.GetCurrentThreadDispatcher();
            }

            public IMessageHandlerThreadContext GetCurrentThreadContext()
            {
                return _inner.GetCurrentThreadContext();
            }

            public IDisposable RegisterManagedThread(IThreadManager manager, int threadId, string purpose)
            {
                var token = _inner.RegisterManagedThread(manager, threadId, purpose);
                _tokens.Add(token);
                return token;
            }

            public void UnregisterManagedThread(int threadId)
            {
                _inner.UnregisterManagedThread(threadId);
            }
        }
    }
}
