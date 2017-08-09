using Acquaintance.Threading;
using System;

namespace Acquaintance.RequestResponse
{
    public class ListenerBuilder<TRequest, TResponse> :
        IChannelListenerBuilder<TRequest, TResponse>,
        IActionListenerBuilder<TRequest, TResponse>,
        IThreadListenerBuilder<TRequest, TResponse>,
        IDetailsListenerBuilder<TRequest, TResponse>
    {
        private readonly IThreadPool _threadPool;
        private readonly IReqResBus _messageBus;

        private DispatchThreadType _dispatchType;
        private int _threadId;
        private int _timeoutMs;
        private IListenerReference<TRequest, TResponse> _funcReference;
        private int _maxRequests;
        private Func<TRequest, bool> _filter;
        private RouteBuilder<TRequest, TResponse> _routeBuilder;
        private bool _useDedicatedThread;
        private int _maxAttempts;
        private int _breakMs;
        private Func<IListener<TRequest, TResponse>, IListener<TRequest, TResponse>> _modify;

        public ListenerBuilder(IReqResBus messageBus, IThreadPool threadPool)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));
            if (threadPool == null)
                throw new ArgumentNullException(nameof(threadPool));

            _messageBus = messageBus;
            _threadPool = threadPool;
            _timeoutMs = 5000;
        }

        public string ChannelName { get; private set; }

        public IListener<TRequest, TResponse> BuildListener()
        {
            var listener = BuildListenerInternal();
            listener = WrapListener(listener, _filter, _maxRequests);
            return listener;
        }

        private IListener<TRequest, TResponse> BuildListenerInternal()
        {
            if (_routeBuilder != null)
                return _routeBuilder.BuildListener();
            if (_funcReference != null)
                return CreateListener(_funcReference, _dispatchType, _threadId, _timeoutMs);
            throw new Exception("No function or routes supplied");
        }

        public IDisposable WrapToken(IDisposable token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));
            if (_useDedicatedThread)
                return new SubscriptionWithDedicatedThreadToken(_threadPool, token, _threadId);
            return token;
        }

        public IActionListenerBuilder<TRequest, TResponse> WithChannelName(string name)
        {
            ChannelName = name;
            return this;
        }

        public IActionListenerBuilder<TRequest, TResponse> OnDefaultChannel()
        {
            ChannelName = string.Empty;
            return this;
        }

        public IThreadListenerBuilder<TRequest, TResponse> Invoke(Func<TRequest, TResponse> listener, bool useWeakReference = false)
        {
            _funcReference = CreateReference(listener, useWeakReference);
            return this;
        }

        public IThreadListenerBuilder<TRequest, TResponse> InvokeEnvelope(Func<Envelope<TRequest>, TResponse> listener, bool useWeakReference = false)
        {
            _funcReference = CreateReference(listener, useWeakReference);
            return this;
        }

        public IThreadListenerBuilder<TRequest, TResponse> Route(Action<RouteBuilder<TRequest, TResponse>> build)
        {
            _routeBuilder = new RouteBuilder<TRequest, TResponse>(_messageBus);
            build(_routeBuilder);
            return this;
        }

        public IThreadListenerBuilder<TRequest, TResponse> TransformRequestTo<TTransformed>(string sourceChannelName, Func<TRequest, TTransformed> transform)
        {
            return Invoke(request =>
            {
                var transformed = transform(request);
                return _messageBus.Request<TTransformed, TResponse>(sourceChannelName, transformed);
            });
        }

        public IThreadListenerBuilder<TRequest, TResponse> TransformResponseFrom<TSource>(string sourceChannelName, Func<TSource, TResponse> transform)
        {
            return Invoke(request =>
            {
                var response = _messageBus.Request<TRequest, TSource>(sourceChannelName, request);
                return transform(response);
            });
        }

        public IDetailsListenerBuilder<TRequest, TResponse> Immediate()
        {
            _dispatchType = DispatchThreadType.Immediate;
            return this;
        }

        public IDetailsListenerBuilder<TRequest, TResponse> OnThread(int threadId)
        {
            _dispatchType = DispatchThreadType.SpecificThread;
            _threadId = threadId;
            return this;
        }

        public IDetailsListenerBuilder<TRequest, TResponse> OnWorkerThread()
        {
            _dispatchType = DispatchThreadType.AnyWorkerThread;
            return this;
        }

        public IDetailsListenerBuilder<TRequest, TResponse> OnThreadPool()
        {
            _dispatchType = DispatchThreadType.ThreadpoolThread;
            return this;
        }

        public IDetailsListenerBuilder<TRequest, TResponse> OnDedicatedThread()
        {
            _dispatchType = DispatchThreadType.ThreadpoolThread;
            _useDedicatedThread = true;
            return this;
        }

        public IDetailsListenerBuilder<TRequest, TResponse> WithTimeout(int timeoutMs)
        {
            if (timeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeoutMs));
            _timeoutMs = timeoutMs;
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

        public IDetailsListenerBuilder<TRequest, TResponse> WithCircuitBreaker(int maxAttempts, int breakMs)
        {
            _maxAttempts = maxAttempts;
            _breakMs = breakMs;
            return this;
        }

        public IDetailsListenerBuilder<TRequest, TResponse> ModifyListener(Func<IListener<TRequest, TResponse>, IListener<TRequest, TResponse>> modify)
        {
            _modify = modify;
            return this;
        }

        private IListener<TRequest, TResponse> CreateListener(IListenerReference<TRequest, TResponse> reference, DispatchThreadType dispatchType, int threadId, int timeoutMs)
        {
            switch (dispatchType)
            {
                case DispatchThreadType.AnyWorkerThread:
                    return new AnyThreadListener<TRequest, TResponse>(reference, _threadPool, timeoutMs);
                case DispatchThreadType.SpecificThread:
                    return new SpecificThreadListener<TRequest, TResponse>(reference, threadId, _threadPool, timeoutMs);
                default:
                    return new ImmediateListener<TRequest, TResponse>(reference);
            }
        }

        private IListener<TRequest, TResponse> WrapListener(IListener<TRequest, TResponse> listener, Func<TRequest, bool> filter, int maxRequests)
        {
            if (_maxAttempts > 0 && _breakMs > 0)
                listener = new CircuitBreakerListener<TRequest, TResponse>(listener, _maxAttempts, _breakMs);
            if (filter != null)
                listener = new FilteredListener<TRequest, TResponse>(listener, filter);
            if (maxRequests > 0)
                listener = new MaxRequestsListener<TRequest, TResponse>(listener, maxRequests);
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
