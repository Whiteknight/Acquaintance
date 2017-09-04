using Acquaintance.Threading;
using System;
using Acquaintance.Utility;

namespace Acquaintance.PubSub
{
    public class SubscriptionBuilder<TPayload> :
        ITopicSubscriptionBuilder<TPayload>,
        IActionSubscriptionBuilder<TPayload>,
        IThreadSubscriptionBuilder<TPayload>,
        IDetailsSubscriptionBuilder<TPayload>
    {
        private readonly IPubSubBus _messageBus;
        private readonly IWorkerPool _workerPool;

        private ISubscriberReference<TPayload> _actionReference;
        private DispatchThreadType _dispatchType;
        private Func<TPayload, bool> _filter;
        private int _maxEvents;
        private int _threadId;
        private bool _useDedicatedThread;
        private Func<ISubscription<TPayload>, ISubscription<TPayload>> _wrap;

        public SubscriptionBuilder(IPubSubBus messageBus, IWorkerPool workerPool)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(workerPool, nameof(workerPool));

            _dispatchType = DispatchThreadType.NoPreference;
            _workerPool = workerPool;
            _messageBus = messageBus;
        }

        public string Topic { get; private set; }

        public ISubscription<TPayload> BuildSubscription()
        {
            if (_useDedicatedThread)
                _threadId = _workerPool.StartDedicatedWorker().ThreadId;

            var subscription = BuildSubscriptionInternal();

            subscription = WrapSubscription(subscription);
            return subscription;
        }

        private ISubscription<TPayload> BuildSubscriptionInternal()
        {
            if (_actionReference != null)
                return CreateSubscription(_actionReference, _dispatchType, _threadId);
            throw new Exception("No action specified");
        }

        public IDisposable WrapToken(IDisposable token)
        {
            Assert.ArgumentNotNull(token, nameof(token));

            if (_useDedicatedThread)
                return new SubscriptionWithDedicatedWorkerToken(_workerPool, token, _threadId);
            return token;
        }

        public IActionSubscriptionBuilder<TPayload> WithTopic(string topic)
        {
            Topic = topic ?? string.Empty;
            return this;
        }

        public IActionSubscriptionBuilder<TPayload> WithDefaultTopic()
        {
            Topic = string.Empty;
            return this;
        }

        public IThreadSubscriptionBuilder<TPayload> Invoke(Action<TPayload> action, bool useWeakReferences = false)
        {
            Assert.ArgumentNotNull(action, nameof(action));

            ValidateDoesNotHaveAction();
            var reference = CreateActionReference(action, useWeakReferences);
            _actionReference = reference;
            return this;
        }

        public IThreadSubscriptionBuilder<TPayload> InvokeEnvelope(Action<Envelope<TPayload>> action, bool useWeakReferences = false)
        {
            Assert.ArgumentNotNull(action, nameof(action));

            ValidateDoesNotHaveAction();
            var reference = CreateActionReference(action, useWeakReferences);
            _actionReference = reference;
            return this;
        }

        public IThreadSubscriptionBuilder<TPayload> Invoke(ISubscriptionHandler<TPayload> handler)
        {
            Assert.ArgumentNotNull(handler, nameof(handler));

            ValidateDoesNotHaveAction();
            _actionReference = new SubscriptionHandlerActionReference<TPayload>(handler);
            return this;
        }

        public IThreadSubscriptionBuilder<TPayload> ActivateAndInvoke<TService>(Func<TPayload, TService> createService, Action<TService, TPayload> handler)
        {
            Assert.ArgumentNotNull(handler, nameof(handler));
            Assert.ArgumentNotNull(createService, nameof(createService));

            ValidateDoesNotHaveAction();

            _actionReference = new ActivatedSubscriberReference<TPayload, TService>(createService, handler);
            return this;
        }

        public IThreadSubscriptionBuilder<TPayload> TransformTo<TOutput>(Func<TPayload, TOutput> transform, string newTopic = null)
        {
            Assert.ArgumentNotNull(transform, nameof(transform));

            return Invoke(payload =>
            {
                var transformed = transform(payload);
                _messageBus.Publish(newTopic, transformed);
            });
        }

        public IDetailsSubscriptionBuilder<TPayload> OnWorker()
        {
            ValidateDoesNotHaveDispatchType();
            _dispatchType = DispatchThreadType.AnyWorkerThread;
            return this;
        }

        public IDetailsSubscriptionBuilder<TPayload> Immediate()
        {
            ValidateDoesNotHaveDispatchType();
            _dispatchType = DispatchThreadType.Immediate;
            return this;
        }

        public IDetailsSubscriptionBuilder<TPayload> OnThread(int threadId)
        {
            Assert.IsInRange(threadId, nameof(threadId), 0, 65355);
            ValidateDoesNotHaveDispatchType();
            _dispatchType = DispatchThreadType.SpecificThread;
            _threadId = threadId;
            return this;
        }

        public IDetailsSubscriptionBuilder<TPayload> OnThreadPool()
        {
            _dispatchType = DispatchThreadType.ThreadpoolThread;
            return this;
        }

        public IDetailsSubscriptionBuilder<TPayload> OnDedicatedWorker()
        {
            ValidateDoesNotHaveDispatchType();
            _dispatchType = DispatchThreadType.SpecificThread;
            _useDedicatedThread = true;
            return this;
        }

        // TODO: Should we have an option where the Filter predicate takes an Envelope<TPayload>?
        public IDetailsSubscriptionBuilder<TPayload> WithFilter(Func<TPayload, bool> filter)
        {
            if (filter == null)
                return this;
            if (_filter == null)
                _filter = filter;
            else
            {
                var oldFilter = _filter;
                _filter = p => oldFilter(p) && filter(p);
            }

            return this;
        }

        public IDetailsSubscriptionBuilder<TPayload> MaximumEvents(int maxEvents)
        {
            Assert.IsInRange(maxEvents, nameof(maxEvents), 0, int.MaxValue);

            _maxEvents = maxEvents;
            return this;
        }

        public IDetailsSubscriptionBuilder<TPayload> ModifySubscription(Func<ISubscription<TPayload>, ISubscription<TPayload>> wrap)
        {
            Assert.ArgumentNotNull(wrap, nameof(wrap));
            if (_wrap == null)
                _wrap = wrap;
            else
            {
                var oldWrap = _wrap;
                _wrap = s => wrap(oldWrap(s));
            }
            return this;
        }

        private void ValidateDoesNotHaveAction()
        {
            if (_actionReference != null)
                throw new Exception("Builder already has a defined action");
        }

        private void ValidateDoesNotHaveDispatchType()
        {
            if (_dispatchType != DispatchThreadType.NoPreference)
                throw new Exception($"Builder is already setup to use dispatch type {_dispatchType}");
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
                case DispatchThreadType.NoPreference:
                    return new AnyThreadPubSubSubscription<TPayload>(actionReference, _workerPool);
                case DispatchThreadType.AnyWorkerThread:
                    return new AnyThreadPubSubSubscription<TPayload>(actionReference, _workerPool);
                case DispatchThreadType.SpecificThread:
                    return new SpecificThreadPubSubSubscription<TPayload>(actionReference, threadId, _workerPool);
                case DispatchThreadType.ThreadpoolThread:
                    return new ThreadPoolThreadSubscription<TPayload>(_workerPool, actionReference);
                case DispatchThreadType.Immediate:
                    return new ImmediatePubSubSubscription<TPayload>(actionReference);
                default:
                    return new AnyThreadPubSubSubscription<TPayload>(actionReference, _workerPool);
            }
        }
    }
}
