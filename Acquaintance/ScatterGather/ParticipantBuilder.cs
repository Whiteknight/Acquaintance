﻿using Acquaintance.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.ScatterGather
{
    public class ParticipantBuilder<TRequest, TResponse> :
        IChannelParticipantBuilder<TRequest, TResponse>,
        IActionParticipantBuilder<TRequest, TResponse>,
        IThreadParticipantBuilder<TRequest, TResponse>,
        IDetailsParticipantBuilder<TRequest, TResponse>
    {
        private readonly IThreadPool _threadPool;
        private readonly IReqResBus _messageBus;

        private DispatchThreadType _dispatchType;
        private int _threadId;
        private int _timeoutMs;
        private IParticipantReference<TRequest, TResponse> _funcReference;
        private int _maxRequests;
        private Func<TRequest, bool> _filter;
        private RouteBuilder<TRequest, TResponse> _routerBuilder;

        public ParticipantBuilder(IReqResBus messageBus, IThreadPool threadPool)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));
            if (threadPool == null)
                throw new ArgumentNullException(nameof(threadPool));

            _messageBus = messageBus;
            _threadPool = threadPool;
        }

        public string ChannelName { get; private set; }

        public IParticipant<TRequest, TResponse> BuildParticipant()
        {
            IParticipant<TRequest, TResponse> participant = null;
            // TODO: OnDedicatedThread

            if (_routerBuilder != null)
                participant = _routerBuilder.BuildParticipant();
            else if (_funcReference != null)
                participant = CreateParticipant(_funcReference, _dispatchType, _threadId, _timeoutMs);

            if (participant == null)
                throw new Exception("No actions defined");

            participant = WrapParticipant(participant, _filter, _maxRequests);
            return participant;
        }

        public IActionParticipantBuilder<TRequest, TResponse> WithChannelName(string name)
        {
            ChannelName = name;
            return this;
        }

        public IActionParticipantBuilder<TRequest, TResponse> OnDefaultChannel()
        {
            ChannelName = string.Empty;
            return this;
        }

        public IThreadParticipantBuilder<TRequest, TResponse> Invoke(Func<TRequest, TResponse> participant, bool useWeakReference = false)
        {
            var reference = CreateReference(r => new[] { participant(r) }, useWeakReference);
            _funcReference = reference;
            return this;
        }

        public IThreadParticipantBuilder<TRequest, TResponse> Invoke(Func<TRequest, IEnumerable<TResponse>> participant, bool useWeakReference = false)
        {
            var reference = CreateReference(participant, useWeakReference);
            _funcReference = reference;
            return this;
        }

        public IThreadParticipantBuilder<TRequest, TResponse> Route(Action<RouteBuilder<TRequest, TResponse>> build)
        {
            if (_routerBuilder != null)
                throw new Exception("Routes already defined");
            _routerBuilder = new RouteBuilder<TRequest, TResponse>(_messageBus);
            build(_routerBuilder);
            return this;
        }

        public IThreadParticipantBuilder<TRequest, TResponse> TransformRequestTo<TTransformed>(string sourceChannelName, Func<TRequest, TTransformed> transform)
        {
            return Invoke(request =>
            {
                var transformed = transform(request);
                return _messageBus.Scatter<TTransformed, TResponse>(sourceChannelName, transformed);
            });
        }

        public IThreadParticipantBuilder<TRequest, TResponse> TransformResponseFrom<TSource>(string sourceChannelName, Func<TSource, TResponse> transform)
        {
            return Invoke(request =>
            {
                var responses = _messageBus.Scatter<TRequest, TSource>(sourceChannelName, request);
                return responses.Select(transform);
            });
        }

        public IDetailsParticipantBuilder<TRequest, TResponse> Immediate()
        {
            _dispatchType = DispatchThreadType.Immediate;
            return this;
        }

        public IDetailsParticipantBuilder<TRequest, TResponse> OnThread(int threadId)
        {
            _dispatchType = DispatchThreadType.SpecificThread;
            _threadId = threadId;
            return this;
        }

        public IDetailsParticipantBuilder<TRequest, TResponse> OnWorkerThread()
        {
            _dispatchType = DispatchThreadType.AnyWorkerThread;
            return this;
        }

        public IDetailsParticipantBuilder<TRequest, TResponse> OnThreadPool()
        {
            _dispatchType = DispatchThreadType.ThreadpoolThread;
            return this;
        }

        public IDetailsParticipantBuilder<TRequest, TResponse> OnDedicatedThread()
        {
            throw new NotImplementedException();
        }

        public IDetailsParticipantBuilder<TRequest, TResponse> WithTimeout(int timeoutMs)
        {
            if (timeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeoutMs));
            _timeoutMs = timeoutMs;
            return this;
        }

        public IDetailsParticipantBuilder<TRequest, TResponse> MaximumRequests(int maxRequests)
        {
            _maxRequests = maxRequests;
            return this;
        }

        public IDetailsParticipantBuilder<TRequest, TResponse> WithFilter(Func<TRequest, bool> filter)
        {
            _filter = filter;
            return this;
        }

        private IParticipant<TRequest, TResponse> CreateParticipant(IParticipantReference<TRequest, TResponse> reference, DispatchThreadType dispatchType, int threadId, int timeoutMs)
        {
            switch (dispatchType)
            {
                case DispatchThreadType.AnyWorkerThread:
                    return new AnyThreadParticipant<TRequest, TResponse>(reference, _threadPool, timeoutMs);
                case DispatchThreadType.SpecificThread:
                    return new SpecificThreadParticipant<TRequest, TResponse>(reference, threadId, _threadPool, timeoutMs);
                default:
                    return new ImmediateParticipant<TRequest, TResponse>(reference);
            }
        }

        private IParticipant<TRequest, TResponse> WrapParticipant(IParticipant<TRequest, TResponse> listener, Func<TRequest, bool> filter, int maxRequests)
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
