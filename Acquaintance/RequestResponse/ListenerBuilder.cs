using Acquaintance.Threading;
using System;

namespace Acquaintance.RequestResponse
{
    public interface IChannelListenerBuilder<TRequest, TResponse>
    {
        IActionListenerBuilder<TRequest, TResponse> WithChannelName(string name);
        IActionListenerBuilder<TRequest, TResponse> OnDefaultChannel();
    }

    public interface IActionListenerBuilder<TRequest, TResponse>
    {
        IThreadListenerBuilder<TRequest, TResponse> Invoke(Func<TRequest, TResponse> listener, bool useWeakReference = false);
        IThreadListenerBuilder<TRequest, TResponse> Route(Action<RouteBuilder<TRequest, TResponse>> build);
        IThreadListenerBuilder<TRequest, TResponse> TransformRequestTo<TTransformed>(string sourceChannelName, Func<TRequest, TTransformed> transform);
        IThreadListenerBuilder<TRequest, TResponse> TransformResponseFrom<TSource>(string sourceChannelName, Func<TSource, TResponse> transform);
    }

    public interface IThreadListenerBuilder<TRequest, TResponse>
    {
        IDetailsListenerBuilder<TRequest, TResponse> Immediate();
        IDetailsListenerBuilder<TRequest, TResponse> OnDedicatedThread();
        IDetailsListenerBuilder<TRequest, TResponse> OnThread(int threadId);
        IDetailsListenerBuilder<TRequest, TResponse> OnThreadPool();
        IDetailsListenerBuilder<TRequest, TResponse> OnWorkerThread();
    }

    public interface IDetailsListenerBuilder<TRequest, TResponse>
    {
        IDetailsListenerBuilder<TRequest, TResponse> MaximumRequests(int maxRequests);
        IDetailsListenerBuilder<TRequest, TResponse> WithFilter(Func<TRequest, bool> filter);
        IDetailsListenerBuilder<TRequest, TResponse> WithTimeout(int timeoutMs);
        IDetailsListenerBuilder<TRequest, TResponse> WithCircuitBreaker(int maxAttempts, int breakMs);
        IDetailsListenerBuilder<TRequest, TResponse> ModifyListener(Func<IListener<TRequest, TResponse>, IListener<TRequest, TResponse>> modify);
    }

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
            IListener<TRequest, TResponse> listener = null;
            if (_routeBuilder != null)
                listener = _routeBuilder.BuildListener();
            else if (_funcReference != null)
                listener = CreateListener(_funcReference, _dispatchType, _threadId, _timeoutMs);

            if (listener == null)
                throw new Exception("No function or routes supplied");

            listener = WrapListener(listener, _filter, _maxRequests);
            return listener;
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
            IListener<TRequest, TResponse> listener;
            switch (dispatchType)
            {
                case DispatchThreadType.AnyWorkerThread:
                    listener = new AnyThreadListener<TRequest, TResponse>(reference, _threadPool, timeoutMs);
                    break;
                case DispatchThreadType.SpecificThread:
                    listener = new SpecificThreadListener<TRequest, TResponse>(reference, threadId, _threadPool, timeoutMs);
                    break;
                default:
                    listener = new ImmediateListener<TRequest, TResponse>(reference);
                    break;
            }

            return listener;
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
                return new WeakListenerReference<TRequest, TResponse>(listener);
            return new StrongListenerReference<TRequest, TResponse>(listener);
        }
    }
}
