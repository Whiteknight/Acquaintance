using Acquaintance.Threading;
using System;
using Acquaintance.Common;
using Acquaintance.Utility;

namespace Acquaintance.RequestResponse
{
    public class ListenerBuilder<TRequest, TResponse> :
        ITopicListenerBuilder<TRequest, TResponse>,
        IActionListenerBuilder<TRequest, TResponse>,
        IThreadListenerBuilder<TRequest, TResponse>,
        IDetailsListenerBuilder<TRequest, TResponse>
    {
        private readonly IThreadPool _threadPool;
        private readonly IReqResBus _messageBus;

        private DispatchThreadType _dispatchType;
        private int _threadId;
        private IListenerReference<TRequest, TResponse> _funcReference;
        private int _maxRequests;
        private Func<TRequest, bool> _filter;
        private bool _useDedicatedThread;
        private Func<IListener<TRequest, TResponse>, IListener<TRequest, TResponse>> _modify;
        private CircuitBreaker _circuitBreaker;

        public ListenerBuilder(IReqResBus messageBus, IThreadPool threadPool)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(threadPool, nameof(threadPool));

            _messageBus = messageBus;
            _threadPool = threadPool;
        }

        public string Topic { get; private set; }

        public IListener<TRequest, TResponse> BuildListener()
        {
            var listener = BuildListenerInternal();
            listener = WrapListener(listener, _filter, _maxRequests);
            return listener;
        }

        private IListener<TRequest, TResponse> BuildListenerInternal()
        {
            if (_funcReference != null)
                return CreateListener(_funcReference, _dispatchType, _threadId);
            throw new Exception("No function or routes supplied");
        }

        public IDisposable WrapToken(IDisposable token)
        {
            Assert.ArgumentNotNull(token, nameof(token));
            if (_useDedicatedThread)
                return new SubscriptionWithDedicatedThreadToken(_threadPool, token, _threadId);
            return token;
        }

        public IActionListenerBuilder<TRequest, TResponse> WithTopic(string name)
        {
            Topic = name;
            return this;
        }

        public IActionListenerBuilder<TRequest, TResponse> WithDefaultTopic()
        {
            Topic = string.Empty;
            return this;
        }

        public IThreadListenerBuilder<TRequest, TResponse> Invoke(Func<TRequest, TResponse> listener, bool useWeakReference = false)
        {
            Assert.ArgumentNotNull(listener, nameof(listener));
            ValidateDoesNotAlreadyHaveAction();
            _funcReference = CreateReference(listener, useWeakReference);
            return this;
        }

        public IThreadListenerBuilder<TRequest, TResponse> InvokeEnvelope(Func<Envelope<TRequest>, TResponse> listener, bool useWeakReference = false)
        {
            Assert.ArgumentNotNull(listener, nameof(listener));
            ValidateDoesNotAlreadyHaveAction();
            _funcReference = CreateReference(listener, useWeakReference);
            return this;
        }

        public IThreadListenerBuilder<TRequest, TResponse> TransformRequestTo<TTransformed>(string sourceTopic, Func<TRequest, TTransformed> transform)
        {
            Assert.ArgumentNotNull(transform, nameof(transform));
            return Invoke(request =>
            {
                var transformed = transform(request);
                var waiter = _messageBus.Request<TTransformed, TResponse>(sourceTopic, transformed);
                waiter.WaitForResponse();
                waiter.ThrowExceptionIfError();
                return waiter.GetResponse();
            });
        }

        public IThreadListenerBuilder<TRequest, TResponse> TransformResponseFrom<TSource>(string sourceTopic, Func<TSource, TResponse> transform)
        {
            Assert.ArgumentNotNull(transform, nameof(transform));
            return Invoke(request =>
            {
                var response = _messageBus.Request<TRequest, TSource>(sourceTopic, request);
                response.WaitForResponse();
                response.ThrowExceptionIfError();
                var payload = response.GetResponse();
                return transform(payload);
            });
        }

        public IDetailsListenerBuilder<TRequest, TResponse> Immediate()
        {
            ValidateDoesNotAlreadyHaveDispatchType();
            _dispatchType = DispatchThreadType.Immediate;
            return this;
        }

        public IDetailsListenerBuilder<TRequest, TResponse> OnThread(int threadId)
        {
            ValidateDoesNotAlreadyHaveDispatchType();
            _dispatchType = DispatchThreadType.SpecificThread;
            _threadId = threadId;
            return this;
        }

        public IDetailsListenerBuilder<TRequest, TResponse> OnWorkerThread()
        {
            ValidateDoesNotAlreadyHaveDispatchType();
            _dispatchType = DispatchThreadType.AnyWorkerThread;
            return this;
        }

        public IDetailsListenerBuilder<TRequest, TResponse> OnThreadPool()
        {
            ValidateDoesNotAlreadyHaveDispatchType();
            _dispatchType = DispatchThreadType.ThreadpoolThread;
            return this;
        }

        public IDetailsListenerBuilder<TRequest, TResponse> OnDedicatedThread()
        {
            ValidateDoesNotAlreadyHaveDispatchType();
            _dispatchType = DispatchThreadType.ThreadpoolThread;
            _useDedicatedThread = true;
            return this;
        }

        public IDetailsListenerBuilder<TRequest, TResponse> MaximumRequests(int maxRequests)
        {
            _maxRequests = maxRequests;
            return this;
        }

        public IDetailsListenerBuilder<TRequest, TResponse> WithFilter(Func<TRequest, bool> filter)
        {
            _filter = filter;
            return this;
        }

        public IDetailsListenerBuilder<TRequest, TResponse> ModifyListener(Func<IListener<TRequest, TResponse>, IListener<TRequest, TResponse>> modify)
        {
            _modify = modify;
            return this;
        }

        public IDetailsListenerBuilder<TRequest, TResponse> WithCircuitBreaker(int maxFailures, int breakMs)
        {
            if (_circuitBreaker != null)
                throw new Exception("Already has a circuit breaker configured");
            _circuitBreaker = new CircuitBreaker(breakMs, maxFailures);
            return this;
        }

        private void ValidateDoesNotAlreadyHaveDispatchType()
        {
            if (_dispatchType != DispatchThreadType.NoPreference)
                throw new Exception($"Thread dispatch type {_dispatchType} has already been configured");
        }

        private void ValidateDoesNotAlreadyHaveAction()
        {
            if (_funcReference != null)
                throw new Exception("Builder already has a defined action reference");
        }

        private IListener<TRequest, TResponse> CreateListener(IListenerReference<TRequest, TResponse> reference, DispatchThreadType dispatchType, int threadId)
        {
            switch (dispatchType)
            {
                case DispatchThreadType.NoPreference:
                    return new AnyThreadListener<TRequest, TResponse>(reference, _threadPool);
                case DispatchThreadType.AnyWorkerThread:
                    return new AnyThreadListener<TRequest, TResponse>(reference, _threadPool);
                case DispatchThreadType.SpecificThread:
                    return new SpecificThreadListener<TRequest, TResponse>(reference, threadId, _threadPool);
                case DispatchThreadType.Immediate:
                    return new ImmediateListener<TRequest, TResponse>(reference);
                case DispatchThreadType.ThreadpoolThread:
                    return new ThreadPoolListener<TRequest, TResponse>(reference, _threadPool);
                default:
                    return new ImmediateListener<TRequest, TResponse>(reference);
            }
        }

        private IListener<TRequest, TResponse> WrapListener(IListener<TRequest, TResponse> listener, Func<TRequest, bool> filter, int maxRequests)
        {
            if (filter != null)
                listener = new FilteredListener<TRequest, TResponse>(listener, filter);
            if (maxRequests > 0)
                listener = new MaxRequestsListener<TRequest, TResponse>(listener, maxRequests);
            if (_circuitBreaker != null)
                listener = new CircuitBreakerListener<TRequest, TResponse>(listener, _circuitBreaker);
            if (_modify != null)
                listener = _modify(listener);
            return listener;
        }

        private IListenerReference<TRequest, TResponse> CreateReference(Func<TRequest, TResponse> listener, bool useWeakReference)
        {
            if (useWeakReference)
                return new PayloadWeakListenerReference<TRequest, TResponse>(listener);
            return new PayloadStrongListenerReference<TRequest, TResponse>(listener);
        }

        private IListenerReference<TRequest, TResponse> CreateReference(Func<Envelope<TRequest>, TResponse> listener, bool useWeakReference)
        {
            if (useWeakReference)
                return new EnvelopeWeakListenerReference<TRequest, TResponse>(listener);
            return new EnvelopeStrongListenerReference<TRequest, TResponse>(listener);
        }
    }
}
