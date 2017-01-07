using Acquaintance.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.RequestResponse
{
    public class ParticipantBuilder<TRequest, TResponse>
    {
        private readonly IThreadPool _threadPool;
        private readonly IReqResBus _messageBus;

        private DispatchThreadType _dispatchType;
        private int _threadId;
        private int _timeoutMs;
        private List<IListenerReference<TRequest, TResponse>> _funcReferences;
        private int _maxRequests;
        private Func<TRequest, bool> _filter;
        private List<RequestRoute<TRequest>> _routes;

        public ParticipantBuilder(IReqResBus messageBus, IThreadPool threadPool)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));
            if (threadPool == null)
                throw new ArgumentNullException(nameof(threadPool));


            _routes = new List<RequestRoute<TRequest>>();
            _funcReferences = new List<IListenerReference<TRequest, TResponse>>();
            _messageBus = messageBus;
            _threadPool = threadPool;
        }

        public string ChannelName { get; private set; }

        public ParticipantBuilder<TRequest, TResponse> WithChannelName(string name)
        {
            ChannelName = name;
            return this;
        }

        public ParticipantBuilder<TRequest, TResponse> Immediate()
        {
            _dispatchType = DispatchThreadType.Immediate;
            return this;
        }

        public ParticipantBuilder<TRequest, TResponse> OnThread(int threadId)
        {
            _dispatchType = DispatchThreadType.SpecificThread;
            _threadId = threadId;
            return this;
        }

        public ParticipantBuilder<TRequest, TResponse> OnWorkerThread()
        {
            _dispatchType = DispatchThreadType.AnyWorkerThread;
            return this;
        }

        public ParticipantBuilder<TRequest, TResponse> OnThreadPool()
        {
            _dispatchType = DispatchThreadType.ThreadpoolThread;
            return this;
        }

        public ParticipantBuilder<TRequest, TResponse> WithTimeout(int timeoutMs)
        {
            if (timeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeoutMs));
            _timeoutMs = timeoutMs;
            return this;
        }

        public ParticipantBuilder<TRequest, TResponse> InvokeFunction(Func<TRequest, TResponse> listener, bool useWeakReference = false)
        {
            var reference = CreateReference(listener, useWeakReference);
            _funcReferences.Add(reference);
            return this;
        }

        public ParticipantBuilder<TRequest, TResponse> MaximumRequests(int maxRequests)
        {
            _maxRequests = maxRequests;
            return this;
        }

        public ParticipantBuilder<TRequest, TResponse> WithFilter(Func<TRequest, bool> filter)
        {
            _filter = filter;
            return this;
        }

        public ParticipantBuilder<TRequest, TResponse> RouteForward(Func<TRequest, bool> predicate, string newChannelName = null)
        {
            _routes.Add(new RequestRoute<TRequest>(newChannelName, predicate));
            return this;
        }

        //public ParticipantBuilder<TRequest, TResponse> TransformRequestTo<TTransformed>(string sourceChannelName, Func<TRequest, TTransformed> transform)
        //{
        //    return InvokeFunction(request =>
        //    {
        //        var transformed = transform(request);
        //        return _messageBus.Scatter<TTransformed, TResponse>(sourceChannelName, transformed);
        //    });
        //}

        //public ParticipantBuilder<TRequest, TResponse> TransformResponseFrom<TSource>(string sourceChannelName, Func<TSource, TResponse> transform)
        //{
        //    return InvokeFunction(request =>
        //    {
        //        var response = _messageBus.Request<TRequest, TSource>(sourceChannelName, request);
        //        return transform(response);
        //    });
        //}

        public IList<IListener<TRequest, TResponse>> BuildParticipants()
        {
            if (!_funcReferences.Any() && !_routes.Any())
                throw new Exception("No function or routes supplied");

            var listeners = new List<IListener<TRequest, TResponse>>();
            foreach (var route in _routes)
            {
                IListener<TRequest, TResponse> listener = new RequestRouter<TRequest, TResponse>(_messageBus, _routes, null);
                listener = WrapListener(listener, _filter, _maxRequests);
                listeners.Add(listener);
            }
            foreach (var func in _funcReferences)
            {
                IListener<TRequest, TResponse> listener = CreateListener(func, _dispatchType, _threadId, _timeoutMs);
                listener = WrapListener(listener, _filter, _maxRequests);
                listeners.Add(listener);
            }

            return listeners;
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
