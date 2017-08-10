﻿using Acquaintance.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using Acquaintance.Utility;

namespace Acquaintance.ScatterGather
{
    public class ParticipantBuilder<TRequest, TResponse> :
        ITopicParticipantBuilder<TRequest, TResponse>,
        IActionParticipantBuilder<TRequest, TResponse>,
        IThreadParticipantBuilder<TRequest, TResponse>,
        IDetailsParticipantBuilder<TRequest, TResponse>
    {
        private readonly IThreadPool _threadPool;
        private readonly IScatterGatherBus _messageBus;

        private DispatchThreadType _dispatchType;
        private int _threadId;
        private int _timeoutMs;
        private IParticipantReference<TRequest, TResponse> _funcReference;
        private int _maxRequests;
        private Func<TRequest, bool> _filter;
        private RouteBuilder<TRequest, TResponse> _routerBuilder;
        private bool _useDedicatedThread;
        private Func<IParticipant<TRequest, TResponse>, IParticipant<TRequest, TResponse>> _modify;

        public ParticipantBuilder(IScatterGatherBus messageBus, IThreadPool threadPool)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(threadPool, nameof(threadPool));

            _messageBus = messageBus;
            _threadPool = threadPool;
            _dispatchType = DispatchThreadType.NoPreference;
            _timeoutMs = 5000;
        }

        public string Topic { get; private set; }

        public IParticipant<TRequest, TResponse> BuildParticipant()
        {
            if (_useDedicatedThread)
                _threadId = _threadPool.StartDedicatedWorker().ThreadId;

            var participant = BuildParticipantInternal();

            participant = WrapParticipant(participant, _filter, _maxRequests);
            return participant;
        }

        private IParticipant<TRequest, TResponse> BuildParticipantInternal()
        {
            if (_routerBuilder != null)
                return _routerBuilder.BuildParticipant();
            if (_funcReference != null)
                return CreateParticipant(_funcReference, _dispatchType, _threadId, _timeoutMs);
            throw new Exception("No actions defined");
        }

        public IDisposable WrapToken(IDisposable token)
        {
            Assert.ArgumentNotNull(token, nameof(token));

            if (_useDedicatedThread)
                return new SubscriptionWithDedicatedThreadToken(_threadPool, token, _threadId);
            return token;
        }

        public IActionParticipantBuilder<TRequest, TResponse> WithTopic(string name)
        {
            Topic = name;
            return this;
        }

        public IActionParticipantBuilder<TRequest, TResponse> WithDefaultTopic()
        {
            Topic = string.Empty;
            return this;
        }

        public IThreadParticipantBuilder<TRequest, TResponse> Invoke(Func<TRequest, TResponse> participant, bool useWeakReference = false)
        {
            Assert.ArgumentNotNull(participant, nameof(participant));
            ValidateDoesNotAlreadyHaveAction();
            var reference = CreateReference(r => new[] { participant(r) }, useWeakReference);
            _funcReference = reference;
            return this;
        }

        public IThreadParticipantBuilder<TRequest, TResponse> Invoke(Func<TRequest, IEnumerable<TResponse>> participant, bool useWeakReference = false)
        {
            Assert.ArgumentNotNull(participant, nameof(participant));
            ValidateDoesNotAlreadyHaveAction();
            var reference = CreateReference(participant, useWeakReference);
            _funcReference = reference;
            return this;
        }

        public IThreadParticipantBuilder<TRequest, TResponse> Route(Action<RouteBuilder<TRequest, TResponse>> build)
        {
            Assert.ArgumentNotNull(build, nameof(build));
            ValidateDoesNotAlreadyHaveAction();
            _routerBuilder = new RouteBuilder<TRequest, TResponse>(_messageBus);
            build(_routerBuilder);
            return this;
        }

        public IThreadParticipantBuilder<TRequest, TResponse> TransformRequestTo<TTransformed>(string sourceChannelName, Func<TRequest, TTransformed> transform)
        {
            Assert.ArgumentNotNull(transform, nameof(transform));
            return Invoke(request =>
            {
                var transformed = transform(request);
                return _messageBus.Scatter<TTransformed, TResponse>(sourceChannelName, transformed);
            });
        }

        public IThreadParticipantBuilder<TRequest, TResponse> TransformResponseFrom<TSource>(string sourceChannelName, Func<TSource, TResponse> transform)
        {
            Assert.ArgumentNotNull(transform, nameof(transform));
            return Invoke(request =>
            {
                var responses = _messageBus.Scatter<TRequest, TSource>(sourceChannelName, request);
                return responses.Select(transform);
            });
        }

        public IDetailsParticipantBuilder<TRequest, TResponse> Immediate()
        {
            ValidateDoesNotReadyHaveDispatchType();
            _dispatchType = DispatchThreadType.Immediate;
            return this;
        }

        public IDetailsParticipantBuilder<TRequest, TResponse> OnThread(int threadId)
        {
            Assert.IsInRange(threadId, nameof(threadId), 1, 65535);
            ValidateDoesNotReadyHaveDispatchType();
            _dispatchType = DispatchThreadType.SpecificThread;
            _threadId = threadId;
            return this;
        }

        public IDetailsParticipantBuilder<TRequest, TResponse> OnWorkerThread()
        {
            ValidateDoesNotReadyHaveDispatchType();
            _dispatchType = DispatchThreadType.AnyWorkerThread;
            return this;
        }

        public IDetailsParticipantBuilder<TRequest, TResponse> OnThreadPool()
        {
            ValidateDoesNotReadyHaveDispatchType();
            _dispatchType = DispatchThreadType.ThreadpoolThread;
            return this;
        }

        public IDetailsParticipantBuilder<TRequest, TResponse> OnDedicatedThread()
        {
            ValidateDoesNotReadyHaveDispatchType();
            _dispatchType = DispatchThreadType.SpecificThread;
            _useDedicatedThread = true;
            return this;
        }

        public IDetailsParticipantBuilder<TRequest, TResponse> WithTimeout(int timeoutMs)
        {
            Assert.IsInRange(timeoutMs, nameof(timeoutMs), 1, int.MaxValue);
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

        public IDetailsParticipantBuilder<TRequest, TResponse> ModifyParticipant(Func<IParticipant<TRequest, TResponse>, IParticipant<TRequest, TResponse>> modify)
        {
            _modify = modify;
            return this;
        }

        private void ValidateDoesNotAlreadyHaveAction()
        {
            if (_funcReference != null)
                throw new Exception("Builder already has an action defined");
            if (_routerBuilder != null)
                throw new Exception("Builder is already setup to use routing. May not define a new action.");
        }

        private void ValidateDoesNotReadyHaveDispatchType()
        {
            if (_dispatchType != DispatchThreadType.NoPreference)
                throw new Exception($"Builder is already configured to use Dispatch Type {_dispatchType}");
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

        private IParticipant<TRequest, TResponse> WrapParticipant(IParticipant<TRequest, TResponse> participant, Func<TRequest, bool> filter, int maxRequests)
        {
            if (filter != null)
                participant = new FilteredParticipant<TRequest, TResponse>(participant, filter);
            if (maxRequests > 0)
                participant = new MaxRequestsParticipant<TRequest, TResponse>(participant, maxRequests);
            if (_modify != null)
                participant = _modify(participant);
            return participant;
        }

        private IParticipantReference<TRequest, TResponse> CreateReference(Func<TRequest, IEnumerable<TResponse>> participant, bool useWeakReference)
        {
            if (useWeakReference)
                return new WeakParticipantReference<TRequest, TResponse>(participant);
            return new StrongParticipantReference<TRequest, TResponse>(participant);
        }
    }
}
