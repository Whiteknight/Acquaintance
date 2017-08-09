using Acquaintance.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.PubSub
{
    public class SubscriptionBuilder<TPayload> :
        IChannelSubscriptionBuilder<TPayload>,
        IActionSubscriptionBuilder<TPayload>,
        IThreadSubscriptionBuilder<TPayload>,
        IDetailsSubscriptionBuilder<TPayload>
    {
        private readonly IPubSubBus _messageBus;
        private readonly IThreadPool _threadPool;

        private ISubscriberReference<TPayload> _actionReference;
        private RouteBuilder<TPayload> _routeBuilder;
        private List<string> _distributionList;
        private DispatchThreadType _dispatchType;
        private Func<TPayload, bool> _filter;
        private int _maxEvents;
        private int _threadId;
        private bool _useDedicatedThread;
        private Func<ISubscription<TPayload>, ISubscription<TPayload>> _wrap;

        public SubscriptionBuilder(IPubSubBus messageBus, IThreadPool threadPool)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));

            if (threadPool == null)
                throw new ArgumentNullException(nameof(threadPool));

            _dispatchType = DispatchThreadType.AnyWorkerThread;
            _threadPool = threadPool;
            _messageBus = messageBus;
        }

        public string ChannelName { get; private set; }

        public ISubscription<TPayload> BuildSubscription()
        {
            if (_useDedicatedThread)
                _threadId = _threadPool.StartDedicatedWorker();

            var subscription = BuildSubscriptionInternal();

            subscription = WrapSubscription(subscription);
            return subscription;
        }

        private ISubscription<TPayload> BuildSubscriptionInternal()
        {
            if (_actionReference != null)
                return CreateSubscription(_actionReference, _dispatchType, _threadId);
            if (_routeBuilder != null)
                return _routeBuilder.BuildSubscription();
            if (_distributionList != null && _distributionList.Any())
                return new RoundRobinDispatchSubscription<TPayload>(_messageBus, _distributionList);
            throw new Exception("No action specified");
        }

        public IDisposable WrapToken(IDisposable token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));
            if (_useDedicatedThread)
                return new SubscriptionWithDedicatedThreadToken(_threadPool, token, _threadId);
            return token;
        }

        public IActionSubscriptionBuilder<TPayload> WithChannelName(string channelName)
        {
            ChannelName = channelName;
            return this;
        }

        public IActionSubscriptionBuilder<TPayload> OnDefaultChannel()
        {
            ChannelName = string.Empty;
            return this;
        }

        public IThreadSubscriptionBuilder<TPayload> Invoke(Action<TPayload> action, bool useWeakReferences = false)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (_actionReference != null)
                throw new Exception("Can only have a single action");
            var reference = CreateActionReference(action, useWeakReferences);
            _actionReference = reference;
            return this;
        }

        public IThreadSubscriptionBuilder<TPayload> InvokeEnvelope(Action<Envelope<TPayload>> action, bool useWeakReferences = false)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (_actionReference != null)
                throw new Exception("Can only have a single action");
            var reference = CreateActionReference(action, useWeakReferences);
            _actionReference = reference;
            return this;
        }

        public IThreadSubscriptionBuilder<TPayload> Invoke(ISubscriptionHandler<TPayload> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (_actionReference != null)
                throw new Exception("Can only have a single action");
            _actionReference = new SubscriptionHandlerActionReference<TPayload>(handler);
            return this;
        }

        public IThreadSubscriptionBuilder<TPayload> ActivateAndInvoke<TService>(Func<TPayload, TService> createService, Action<TService, TPayload> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (createService == null)
                throw new ArgumentNullException(nameof(createService));
            if (_actionReference != null)
                throw new Exception("Can only have a single action");

            _actionReference = new ActivatedSubscriberReference<TPayload, TService>(createService, handler);
            return this;
        }

        public IThreadSubscriptionBuilder<TPayload> TransformTo<TOutput>(Func<TPayload, TOutput> transform, string newChannelName = null)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            return Invoke(payload =>
            {
                var transformed = transform(payload);
                _messageBus.Publish(newChannelName, transformed);
            });
        }

        public IThreadSubscriptionBuilder<TPayload> Route(Action<RouteBuilder<TPayload>> build)
        {
            if (_routeBuilder != null)
                throw new Exception("Routing is already setup");
            _routeBuilder = new RouteBuilder<TPayload>(_messageBus);
            build(_routeBuilder);
            return this;
        }

        public IThreadSubscriptionBuilder<TPayload> Distribute(IEnumerable<string> channels)
        {
            if (_distributionList != null)
                throw new Exception("Distribution list is already setup");
            _distributionList = channels.ToList();
            return this;
        }

        public IDetailsSubscriptionBuilder<TPayload> OnWorkerThread()
        {
            _dispatchType = DispatchThreadType.AnyWorkerThread;
            return this;
        }

        public IDetailsSubscriptionBuilder<TPayload> Immediate()
        {
            _dispatchType = DispatchThreadType.Immediate;
            return this;
        }

        public IDetailsSubscriptionBuilder<TPayload> OnThread(int threadId)
        {
            _dispatchType = DispatchThreadType.SpecificThread;
            _threadId = threadId;
            return this;
        }

        public IDetailsSubscriptionBuilder<TPayload> OnThreadPool()
        {
            _dispatchType = DispatchThreadType.ThreadpoolThread;
            return this;
        }

        public IDetailsSubscriptionBuilder<TPayload> OnDedicatedThread()
        {
            _dispatchType = DispatchThreadType.SpecificThread;
            _useDedicatedThread = true;
            return this;
        }

        // TODO: Should we have an option where the Filter predicate takes an Envelope<TPayload>?
        public IDetailsSubscriptionBuilder<TPayload> WithFilter(Func<TPayload, bool> filter)
        {
            _filter = filter;
            return this;
        }

        public IDetailsSubscriptionBuilder<TPayload> MaximumEvents(int maxEvents)
        {
            _maxEvents = maxEvents;
            return this;
        }

        public IDetailsSubscriptionBuilder<TPayload> ModifySubscription(Func<ISubscription<TPayload>, ISubscription<TPayload>> wrap)
        {
            _wrap = wrap;
            return this;
        }

        private ISubscription<TPayload> WrapSubscription(ISubscription<TPayload> subscription)
        {
            if (_filter != null)
                subscription = new FilteredSubscription<TPayload>(subscription, _filter);
            if (_maxEvents > 0)
                subscription = new MaxEventsSubscription<TPayload>(subscription, _maxEvents);
            if (_wrap != null)
                subscription = _wrap(subscription);
            return subscription;
        }

        private static ISubscriberReference<TPayload> CreateActionReference(Action<Envelope<TPayload>> act, bool useWeakReferences)
        {
            if (useWeakReferences)
                return new EnvelopeWeakSubscriberReference<TPayload>(act);
            return new EnvelopeStrongSubscriberReference<TPayload>(act);
        }

        private static ISubscriberReference<TPayload> CreateActionReference(Action<TPayload> act, bool useWeakReferences)
        {
            if (useWeakReferences)
                return new PayloadWeakSubscriberReference<TPayload>(act);
            return new PayloadStrongSubscriberReference<TPayload>(act);
        }

        private ISubscription<TPayload> CreateSubscription(ISubscriberReference<TPayload> actionReference, DispatchThreadType dispatchType, int threadId)
        {
            switch (dispatchType)
            {
                case DispatchThreadType.AnyWorkerThread:
                    return new AnyThreadPubSubSubscription<TPayload>(actionReference, _threadPool);
                case DispatchThreadType.SpecificThread:
                    return new SpecificThreadPubSubSubscription<TPayload>(actionReference, threadId, _threadPool);
                case DispatchThreadType.ThreadpoolThread:
                    return new ThreadPoolThreadSubscription<TPayload>(_threadPool, actionReference);
                case DispatchThreadType.Immediate:
                    return new ImmediatePubSubSubscription<TPayload>(actionReference);
                default:
                    return new AnyThreadPubSubSubscription<TPayload>(actionReference, _threadPool);
            }
        }
    }
}
