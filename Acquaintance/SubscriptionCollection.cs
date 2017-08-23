using Acquaintance.PubSub;
using Acquaintance.RequestResponse;
using Acquaintance.ScatterGather;
using Acquaintance.Threading;
using System;
using Acquaintance.Routing;
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
        private readonly DisposableTopicRouter _router;

        public SubscriptionCollection(IMessageBus messageBus)
        {
            _messageBus = messageBus;
            _subscriptions = new DisposableCollection();
            ThreadPool = new DisposableThreadPool(messageBus.ThreadPool, _subscriptions);
            _router = new DisposableTopicRouter(messageBus.PublishRouter, messageBus.RequestRouter, messageBus.ScatterRouter, _subscriptions);
        }

        public IPublishTopicRouter PublishRouter => _router;
        public IRequestTopicRouter RequestRouter => _router;
        public IScatterTopicRouter ScatterRouter => _router;

        public IThreadPool ThreadPool { get; }

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

        public IScatter<TResponse> ScatterEnvelope<TRequest, TResponse>(Envelope<TRequest> envelope)
        {
            return _messageBus.ScatterEnvelope<TRequest, TResponse>(envelope);
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

        private class DisposableTopicRouter : IPublishTopicRouter, IRequestTopicRouter, IScatterTopicRouter
        {
            private readonly IPublishTopicRouter _publishRouter;
            private readonly IRequestTopicRouter _requestRouter;
            private readonly IScatterTopicRouter _scatterRouter;
            private readonly DisposableCollection _tokens;

            public DisposableTopicRouter(IPublishTopicRouter publishRouter, IRequestTopicRouter requestRouter, IScatterTopicRouter scatterRouter, DisposableCollection tokens)
            {
                _publishRouter = publishRouter;
                _requestRouter = requestRouter;
                _scatterRouter = scatterRouter;
                _tokens = tokens;
            }

            public string[] RoutePublish<TPayload>(string topic, Envelope<TPayload> envelope)
            {
                return _publishRouter.RoutePublish(topic, envelope);
            }

            public IDisposable AddRule<TPayload>(string topic, IPublishRouteRule<TPayload> rule)
            {
                var token = _publishRouter.AddRule(topic, rule);
                _tokens.Add(token);
                return token;
            }

            public string RouteRequest<TRequest, TResponse>(string topic, Envelope<TRequest> envelope)
            {
                return _requestRouter.RouteRequest<TRequest, TResponse>(topic, envelope);
            }

            public IDisposable AddRule<TRequest, TResponse>(string topic, IRequestRouteRule<TRequest> rule)
            {
                var token = _requestRouter.AddRule<TRequest, TResponse>(topic, rule);
                _tokens.Add(token);
                return token;
            }

            public string RouteScatter<TRequest, TResponse>(string topic, Envelope<TRequest> envelope)
            {
                return _scatterRouter.RouteScatter<TRequest, TResponse>(topic, envelope);
            }

            public IDisposable AddRule<TRequest, TResponse>(string topic, IScatterRouteRule<TRequest> rule)
            {
                var token = _scatterRouter.AddRule<TRequest, TResponse>(topic, rule);
                _tokens.Add(token);
                return token;
            }
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
