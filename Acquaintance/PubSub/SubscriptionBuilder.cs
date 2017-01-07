using Acquaintance.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.PubSub
{
    public class SubscriptionBuilder<TPayload>
    {
        private readonly IPubSubBus _messageBus;
        private readonly IThreadPool _threadPool;

        private List<ISubscriberReference<TPayload>> _actionReferences;
        private readonly List<EventRoute<TPayload>> _routes;
        private DispatchThreadType _dispatchType;
        private Func<TPayload, bool> _filter;
        private int _maxEvents;
        private int _threadId;

        public SubscriptionBuilder(IPubSubBus messageBus, IThreadPool threadPool)
        {
            _dispatchType = DispatchThreadType.AnyWorkerThread;
            _threadPool = threadPool;
            _messageBus = messageBus;
            _routes = new List<EventRoute<TPayload>>();
            _actionReferences = new List<ISubscriberReference<TPayload>>();
        }

        public string ChannelName { get; private set; }

        public IReadOnlyList<ISubscription<TPayload>> BuildSubscriptions()
        {
            if (!_actionReferences.Any() && !_routes.Any())
                throw new Exception("No actions and no routes set");

            var subscriptions = new List<ISubscription<TPayload>>();
            foreach (var action in _actionReferences)
            {
                var subscription = CreateSubscription(action, _dispatchType, _threadId);
                subscription = WrapSubscription(subscription);
                subscriptions.Add(subscription);
            }

            foreach (var route in _routes)
            {
                var subscription = CreateRouterSubscription(route);
                subscription = WrapSubscription(subscription);
                subscriptions.Add(subscription);
            }

            return subscriptions;
        }

        private ISubscription<TPayload> CreateRouterSubscription(EventRoute<TPayload> route)
        {
            var reference = CreateActionReference(payload =>
            {
                _messageBus.Publish(route.ChannelName, payload);
            }, false);
            ISubscription<TPayload> subscription = new ImmediatePubSubSubscription<TPayload>(reference);
            if (route.Predicate != null)
                subscription = new FilteredSubscription<TPayload>(subscription, route.Predicate);
            return subscription;
        }

        private ISubscription<TPayload> WrapSubscription(ISubscription<TPayload> subscription)
        {
            if (_filter != null)
                subscription = new FilteredSubscription<TPayload>(subscription, _filter);
            if (_maxEvents > 0)
                subscription = new MaxEventsSubscription<TPayload>(subscription, _maxEvents);
            return subscription;
        }

        public SubscriptionBuilder<TPayload> InvokeAction(Action<TPayload> action, bool useWeakReferences = true)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            var reference = CreateActionReference(action, useWeakReferences);
            _actionReferences.Add(reference);
            return this;
        }

        public SubscriptionBuilder<TPayload> TransformTo<TOutput>(Func<TPayload, TOutput> transform, string newChannelName = null)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            return InvokeAction(payload =>
            {
                var transformed = transform(payload);
                _messageBus.Publish(newChannelName, transformed);
            });
        }

        private static ISubscriberReference<TPayload> CreateActionReference(Action<TPayload> act, bool useWeakReferences)
        {
            if (useWeakReferences)
                return new WeakSubscriberReference<TPayload>(act);
            return new StrongSubscriberReference<TPayload>(act);
        }

        private ISubscription<TPayload> CreateSubscription(ISubscriberReference<TPayload> actionReference, DispatchThreadType dispatchType, int threadId)
        {
            ISubscription<TPayload> subscription;
            switch (dispatchType)
            {
                case DispatchThreadType.AnyWorkerThread:
                    subscription = new AnyThreadPubSubSubscription<TPayload>(actionReference, _threadPool);
                    break;
                case DispatchThreadType.SpecificThread:
                    subscription = new SpecificThreadPubSubSubscription<TPayload>(actionReference, threadId, _threadPool);
                    break;
                case DispatchThreadType.ThreadpoolThread:
                    subscription = new ThreadPoolThreadSubscription<TPayload>(actionReference);
                    break;
                case DispatchThreadType.Immediate:
                    subscription = new ImmediatePubSubSubscription<TPayload>(actionReference);
                    break;
                default:
                    subscription = CreateDefaultSubscription(actionReference, _threadPool);
                    break;
            }
            return subscription;
        }

        private static ISubscription<TPayload> CreateDefaultSubscription(ISubscriberReference<TPayload> actionReference, IThreadPool threadPool)
        {
            if (threadPool != null && threadPool.NumberOfRunningFreeWorkers > 0)
                return new AnyThreadPubSubSubscription<TPayload>(actionReference, threadPool);
            return new ThreadPoolThreadSubscription<TPayload>(actionReference);
        }

        public SubscriptionBuilder<TPayload> WithChannelName(string name)
        {
            ChannelName = name;
            return this;
        }

        public SubscriptionBuilder<TPayload> WithFilter(Func<TPayload, bool> filter)
        {
            _filter = filter;
            return this;
        }

        public SubscriptionBuilder<TPayload> MaximumEvents(int maxEvents)
        {
            _maxEvents = maxEvents;
            return this;
        }

        public SubscriptionBuilder<TPayload> OnWorkerThread()
        {
            _dispatchType = Threading.DispatchThreadType.AnyWorkerThread;
            return this;
        }

        public SubscriptionBuilder<TPayload> Immediate()
        {
            _dispatchType = Threading.DispatchThreadType.Immediate;
            return this;
        }

        public SubscriptionBuilder<TPayload> OnThread(int threadId)
        {
            _dispatchType = Threading.DispatchThreadType.SpecificThread;
            _threadId = threadId;
            return this;
        }

        public SubscriptionBuilder<TPayload> OnThreadPool()
        {
            _dispatchType = Threading.DispatchThreadType.ThreadpoolThread;
            return this;
        }

        public SubscriptionBuilder<TPayload> RouteForward(Func<TPayload, bool> predicate, string newChannelName)
        {
            _routes.Add(new EventRoute<TPayload>(newChannelName, predicate));
            return this;
        }

    }
}
