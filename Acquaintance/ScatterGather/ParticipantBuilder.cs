using Acquaintance.RequestResponse;
using Acquaintance.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.ScatterGather
{
    public class ParticipantBuilder<TRequest, TResponse>
    {
        private readonly IThreadPool _threadPool;
        private readonly IReqResBus _messageBus;

        private DispatchThreadType _dispatchType;
        private int _threadId;
        private int _timeoutMs;
        private readonly List<IParticipantReference<TRequest, TResponse>> _funcReferences;
        private int _maxRequests;
        private Func<TRequest, bool> _filter;
        private readonly List<RequestRoute<TRequest>> _routes;

        public ParticipantBuilder(IReqResBus messageBus, IThreadPool threadPool)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));
            if (threadPool == null)
                throw new ArgumentNullException(nameof(threadPool));


            _routes = new List<RequestRoute<TRequest>>();
            _funcReferences = new List<IParticipantReference<TRequest, TResponse>>();
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

        public ParticipantBuilder<TRequest, TResponse> InvokeFunction(Func<TRequest, TResponse> participant, bool useWeakReference = false)
        {
            var reference = CreateReference(r => new[] { participant(r) }, useWeakReference);
            _funcReferences.Add(reference);
            return this;
        }

        public ParticipantBuilder<TRequest, TResponse> InvokeFunction(Func<TRequest, IEnumerable<TResponse>> participant, bool useWeakReference = false)
        {
            var reference = CreateReference(participant, useWeakReference);
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

        public IList<IParticipant<TRequest, TResponse>> BuildParticipants()
        {
            if (!_funcReferences.Any() && !_routes.Any())
                throw new Exception("No function or routes supplied");

            var listeners = new List<IParticipant<TRequest, TResponse>>();
            foreach (var route in _routes)
            {
                IParticipant<TRequest, TResponse> listener = new ScatterRouter<TRequest, TResponse>(_messageBus, _routes);
                listener = WrapListener(listener, _filter, _maxRequests);
                listeners.Add(listener);
            }
            foreach (var func in _funcReferences)
            {
                IParticipant<TRequest, TResponse> listener = CreateListener(func, _dispatchType, _threadId, _timeoutMs);
                listener = WrapListener(listener, _filter, _maxRequests);
                listeners.Add(listener);
            }

            return listeners;
        }

        private IParticipant<TRequest, TResponse> CreateListener(IParticipantReference<TRequest, TResponse> reference, DispatchThreadType dispatchType, int threadId, int timeoutMs)
        {
            IParticipant<TRequest, TResponse> listener;
            switch (dispatchType)
            {
                case DispatchThreadType.AnyWorkerThread:
                    listener = new AnyThreadParticipant<TRequest, TResponse>(reference, _threadPool, timeoutMs);
                    break;
                case DispatchThreadType.SpecificThread:
                    listener = new SpecificThreadParticipant<TRequest, TResponse>(reference, threadId, _threadPool, timeoutMs);
                    break;
                default:
                    listener = new ImmediateParticipant<TRequest, TResponse>(reference);
                    break;
            }

            return listener;
        }

        private IParticipant<TRequest, TResponse> WrapListener(IParticipant<TRequest, TResponse> listener, Func<TRequest, bool> filter, int maxRequests)
        {
            if (filter != null)
                listener = new FilteredParticipant<TRequest, TResponse>(listener, filter);
            if (maxRequests > 0)
                listener = new MaxRequestsParticipant<TRequest, TResponse>(listener, maxRequests);
            return listener;
        }

        private IParticipantReference<TRequest, TResponse> CreateReference(Func<TRequest, IEnumerable<TResponse>> listener, bool useWeakReference)
        {
            if (useWeakReference)
                return new WeakParticipantReference<TRequest, TResponse>(listener);
            return new StrongParticipantReference<TRequest, TResponse>(listener);
        }
    }
}
