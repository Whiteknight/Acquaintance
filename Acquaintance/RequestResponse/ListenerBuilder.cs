using Acquaintance.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.RequestResponse
{
    public class ListenerBuilder<TRequest, TResponse>
    {
        private readonly IThreadPool _threadPool;
        private readonly IReqResBus _messageBus;

        private DispatchThreadType _dispatchType;
        private int _threadId;
        private int _timeoutMs;
        private IListenerReference<TRequest, TResponse> _funcReference;
        private int _maxRequests;
        private Func<TRequest, bool> _filter;
        private readonly List<RequestRoute<TRequest>> _routes;

        public ListenerBuilder(IReqResBus messageBus, IThreadPool threadPool)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));
            if (threadPool == null)
                throw new ArgumentNullException(nameof(threadPool));

            _routes = new List<RequestRoute<TRequest>>();
            _messageBus = messageBus;
            _threadPool = threadPool;
            _timeoutMs = 5000;
        }

        public string ChannelName { get; private set; }

        public ListenerBuilder<TRequest, TResponse> WithChannelName(string name)
        {
            ChannelName = name;
            return this;
        }

        public ListenerBuilder<TRequest, TResponse> Immediate()
        {
            _dispatchType = DispatchThreadType.Immediate;
            return this;
        }

        public ListenerBuilder<TRequest, TResponse> OnThread(int threadId)
        {
            _dispatchType = DispatchThreadType.SpecificThread;
            _threadId = threadId;
            return this;
        }

        public ListenerBuilder<TRequest, TResponse> OnWorkerThread()
        {
            _dispatchType = DispatchThreadType.AnyWorkerThread;
            return this;
        }

        public ListenerBuilder<TRequest, TResponse> OnThreadPool()
        {
            _dispatchType = DispatchThreadType.ThreadpoolThread;
            return this;
        }

        public ListenerBuilder<TRequest, TResponse> WithTimeout(int timeoutMs)
        {
            if (timeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeoutMs));
            _timeoutMs = timeoutMs;
            return this;
        }

        public ListenerBuilder<TRequest, TResponse> InvokeFunction(Func<TRequest, TResponse> listener, bool useWeakReference = false)
        {
            _funcReference = CreateReference(listener, useWeakReference);
            return this;
        }

        public ListenerBuilder<TRequest, TResponse> MaximumRequests(int maxRequests)
        {
            _maxRequests = maxRequests;
            return this;
        }

        public ListenerBuilder<TRequest, TResponse> WithFilter(Func<TRequest, bool> filter)
        {
            _filter = filter;
            return this;
        }

        public ListenerBuilder<TRequest, TResponse> RouteForward(Func<TRequest, bool> predicate, string newChannelName = null)
        {
            _routes.Add(new RequestRoute<TRequest>(newChannelName, predicate));
            return this;
        }

        public ListenerBuilder<TRequest, TResponse> TransformRequestTo<TTransformed>(string sourceChannelName, Func<TRequest, TTransformed> transform)
        {
            return InvokeFunction(request =>
            {
                var transformed = transform(request);
                return _messageBus.Request<TTransformed, TResponse>(sourceChannelName, transformed);
            });
        }

        public ListenerBuilder<TRequest, TResponse> TransformResponseFrom<TSource>(string sourceChannelName, Func<TSource, TResponse> transform)
        {
            return InvokeFunction(request =>
            {
                var response = _messageBus.Request<TRequest, TSource>(sourceChannelName, request);
                return transform(response);
            });
        }

        public IListener<TRequest, TResponse> BuildListener()
        {
            if (_funcReference == null && !_routes.Any())
                throw new Exception("No function or routes supplied");

            IListener<TRequest, TResponse> listener = null;
            if (_routes.Any())
                listener = new RequestRouter<TRequest, TResponse>(_messageBus, _routes, _funcReference);
            else if (_funcReference != null)
                listener = CreateListener(_funcReference, _dispatchType, _threadId, _timeoutMs);

            listener = WrapListener(listener, _filter, _maxRequests);
            return listener;
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
            if (filter != null)
                listener = new FilteredListener<TRequest, TResponse>(listener, filter);
            if (maxRequests > 0)
                listener = new MaxRequestsListener<TRequest, TResponse>(listener, maxRequests);
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
