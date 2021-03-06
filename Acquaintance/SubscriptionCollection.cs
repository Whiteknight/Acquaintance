﻿using Acquaintance.PubSub;
using Acquaintance.RequestResponse;
using Acquaintance.ScatterGather;
using Acquaintance.Threading;
using System;
using Acquaintance.Logging;
using Acquaintance.Modules;
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
            WorkerPool = new DisposableWorkerPool(messageBus.WorkerPool, _subscriptions);
            _router = new DisposableTopicRouter(messageBus.PublishRouter, messageBus.RequestRouter, messageBus.ScatterRouter, _subscriptions);
            Modules = new ReadOnlyModules(messageBus);
        }

        public ILogger Logger => _messageBus.Logger;
        public string Id => _messageBus.Id;
        public IPublishTopicRouter PublishRouter => _router;
        public IRequestTopicRouter RequestRouter => _router;
        public IScatterTopicRouter ScatterRouter => _router;

        public IModuleManager Modules { get; }
        public IWorkerPool WorkerPool { get; }

        public IEnvelopeFactory EnvelopeFactory => _messageBus.EnvelopeFactory;

        public IDisposable Subscribe<TPayload>(string[] topics, ISubscription<TPayload> subscription)
        {
            var token = _messageBus.Subscribe(topics, subscription);
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
            _subscriptions.Clear();
        }

        public string[] ReportContents()
        {
            return _subscriptions.ToStringArray();
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }

        private class ReadOnlyModules : IModuleManager
        {
            private readonly IMessageBus _messageBus;

            public ReadOnlyModules(IMessageBus messageBus)
            {
                _messageBus = messageBus;
            }

            public IDisposable Add<TModule>(TModule module)
                where TModule : class, IMessageBusModule
            {
                throw new Exception("Cannot add a module to a child bus");
            }

            public TModule Get<TModule>()
                where TModule : class, IMessageBusModule
            {
                return _messageBus.Modules.Get<TModule>();
            }
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

            public string[] RoutePublish<TPayload>(string[] topics, Envelope<TPayload> envelope)
            {
                return _publishRouter.RoutePublish(topics, envelope);
            }

            public IDisposable AddPublishRouteRule<TPayload>(string[] topics, IRouteRule<TPayload> rule)
            {
                var token = _publishRouter.AddPublishRouteRule(topics, rule);
                _tokens.Add(token);
                return token;
            }

            public string RouteRequest<TRequest, TResponse>(string topic, Envelope<TRequest> envelope)
            {
                return _requestRouter.RouteRequest<TRequest, TResponse>(topic, envelope);
            }

            public IDisposable AddRequestRouteRule<TRequest, TResponse>(string topic, IRouteRule<TRequest> rule)
            {
                var token = _requestRouter.AddRequestRouteRule<TRequest, TResponse>(topic, rule);
                _tokens.Add(token);
                return token;
            }

            public string RouteScatter<TRequest, TResponse>(string topic, Envelope<TRequest> envelope)
            {
                return _scatterRouter.RouteScatter<TRequest, TResponse>(topic, envelope);
            }

            public IDisposable AddScatterRouteRule<TRequest, TResponse>(string topic, IRouteRule<TRequest> rule)
            {
                var token = _scatterRouter.AddScatterRouteRule<TRequest, TResponse>(topic, rule);
                _tokens.Add(token);
                return token;
            }
        }

        private class DisposableWorkerPool : IWorkerPool
        {
            private readonly IWorkerPool _inner;
            private readonly DisposableCollection _tokens;

            public DisposableWorkerPool(IWorkerPool inner, DisposableCollection tokens)
            {
                _inner = inner;
                _tokens = tokens;
            }

            public int NumberOfRunningFreeWorkers => _inner.NumberOfRunningFreeWorkers;

            public ThreadReport GetThreadReport()
            {
                return _inner.GetThreadReport();
            }

            public WorkerToken StartDedicatedWorker()
            {
                var token = _inner.StartDedicatedWorker();
                _tokens.Add(token);
                return token;
            }

            public void StopDedicatedWorker(int threadId)
            {
                _inner.StopDedicatedWorker(threadId);
            }

            public IActionDispatcher GetDispatcher(int threadId, bool allowAutoCreate)
            {
                return _inner.GetDispatcher(threadId, allowAutoCreate);
            }

            public IActionDispatcher GetFreeWorkerDispatcher()
            {
                return _inner.GetFreeWorkerDispatcher();
            }

            public IActionDispatcher GetThreadPoolDispatcher()
            {
                return _inner.GetThreadPoolDispatcher();
            }

            public IActionDispatcher GetAnyWorkerDispatcher()
            {
                return _inner.GetAnyWorkerDispatcher();
            }

            public IActionDispatcher GetCurrentThreadDispatcher()
            {
                return _inner.GetCurrentThreadDispatcher();
            }

            public IWorkerContext GetCurrentThreadContext()
            {
                return _inner.GetCurrentThreadContext();
            }

            public IDisposable RegisterManagedThread(string owner, int threadId, string purpose)
            {
                var token = _inner.RegisterManagedThread(owner, threadId, purpose);
                _tokens.Add(token);
                return token;
            }
        }
    }
}
