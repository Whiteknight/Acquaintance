using Acquaintance.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.PubSub
{
    public interface IChannelSubscriptionBuilder<TPayload>
    {
        IActionSubscriptionBuilder<TPayload> WithChannelName(string name);
        IActionSubscriptionBuilder<TPayload> OnDefaultChannel();
    }

    public interface IActionSubscriptionBuilder<TPayload>
    {
        IDetailsSubscriptionBuilder<TPayload> InvokeAction(Action<TPayload> action, bool useWeakReferences = true);
        IDetailsSubscriptionBuilder<TPayload> TransformTo<TOutput>(Func<TPayload, TOutput> transform, string newChannelName = null);
        IDetailsSubscriptionBuilder<TPayload> Route(Action<RouteBuilder<TPayload>> build);
        IDetailsSubscriptionBuilder<TPayload> Distribute(IEnumerable<string> channels);
    }

    public interface IDetailsSubscriptionBuilder<TPayload>
    {
        IDetailsSubscriptionBuilder<TPayload> WithFilter(Func<TPayload, bool> filter);
        IDetailsSubscriptionBuilder<TPayload> MaximumEvents(int maxEvents);

        IDetailsSubscriptionBuilder<TPayload> OnWorkerThread();
        IDetailsSubscriptionBuilder<TPayload> Immediate();
        IDetailsSubscriptionBuilder<TPayload> OnThread(int threadId);
        IDetailsSubscriptionBuilder<TPayload> OnThreadPool();
        IDetailsSubscriptionBuilder<TPayload> OnDedicatedThread();
    }

    public class SubscriptionBuilder<TPayload> : IChannelSubscriptionBuilder<TPayload>, IActionSubscriptionBuilder<TPayload>, IDetailsSubscriptionBuilder<TPayload>
    {
        private readonly IPubSubBus _messageBus;
        private readonly IThreadPool _threadPool;

        private ISubscriberReference<TPayload> _actionReference;
        private readonly List<EventRoute<TPayload>> _routes;
        private List<string> _distributionList;
        private DispatchThreadType _dispatchType;
        private Func<TPayload, bool> _filter;
        private int _maxEvents;
        private int _threadId;
        private bool _useDedicatedThread;

        public SubscriptionBuilder(IPubSubBus messageBus, IThreadPool threadPool)
        {
            _dispatchType = DispatchThreadType.AnyWorkerThread;
            _threadPool = threadPool;
            _messageBus = messageBus;
            _routes = new List<EventRoute<TPayload>>();
        }

        public string ChannelName { get; private set; }

        public ISubscription<TPayload> BuildSubscription()
        {
            if (_useDedicatedThread)
                _threadId = _threadPool.StartDedicatedWorker();

            ISubscription<TPayload> subscription = null;
            if (_actionReference != null)
                subscription = CreateSubscription(_actionReference, _dispatchType, _threadId);
            else if (_routes.Any())
                subscription = new RoutingSubscription<TPayload>(_messageBus, _routes);
            else if (_distributionList != null && _distributionList.Any())
                subscription = new RoundRobinDispatchSubscription<TPayload>(_messageBus, _distributionList);

            if (subscription == null)
                throw new Exception("No action specified");

            subscription = WrapSubscription(subscription);
            return subscription;
        }

        public IDisposable WrapToken(IDisposable token)
        {
            if (_useDedicatedThread)
                return new SubscriptionWithDedicatedThreadToken(_threadPool, token, _threadId);
            return token;
        }

        public IActionSubscriptionBuilder<TPayload> WithChannelName(string name)
        {
            ChannelName = name;
            return this;
        }

        public IActionSubscriptionBuilder<TPayload> OnDefaultChannel()
        {
            ChannelName = string.Empty;
            return this;
        }

        public IDetailsSubscriptionBuilder<TPayload> InvokeAction(Action<TPayload> action, bool useWeakReferences = true)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (_actionReference != null)
                throw new Exception("Can only have a single action");
            var reference = CreateActionReference(action, useWeakReferences);
            _actionReference = reference;
            return this;
        }

        public IDetailsSubscriptionBuilder<TPayload> TransformTo<TOutput>(Func<TPayload, TOutput> transform, string newChannelName = null)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            return InvokeAction(payload =>
            {
                var transformed = transform(payload);
                _messageBus.Publish(newChannelName, transformed);
            });
        }

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

        public IDetailsSubscriptionBuilder<TPayload> Route(Action<RouteBuilder<TPayload>> build)
        {
            var builder = new RouteBuilder<TPayload>(_routes);
            build(builder);
            return this;
        }

        public IDetailsSubscriptionBuilder<TPayload> Distribute(IEnumerable<string> channels)
        {
            if (_distributionList != null)
                throw new Exception("Distribution list is already setup");
            _distributionList = channels.ToList();
            return this;
        }

        private ISubscription<TPayload> WrapSubscription(ISubscription<TPayload> subscription)
        {
            if (_filter != null)
                subscription = new FilteredSubscription<TPayload>(subscription, _filter);
            if (_maxEvents > 0)
                subscription = new MaxEventsSubscription<TPayload>(subscription, _maxEvents);
            return subscription;
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
                    subscription = new ThreadPoolThreadSubscription<TPayload>(_threadPool, actionReference);
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
            return new AnyThreadPubSubSubscription<TPayload>(actionReference, threadPool);
        }
    }
}
