﻿using Acquaintance.Threading;
using System;
using Acquaintance.Utility;

namespace Acquaintance.ScatterGather
{
    public class ParticipantBuilder<TRequest, TResponse> :
        ITopicParticipantBuilder<TRequest, TResponse>,
        IActionParticipantBuilder<TRequest, TResponse>,
        IThreadParticipantBuilder<TRequest, TResponse>,
        IDetailsParticipantBuilder<TRequest, TResponse>
    {
        private readonly IWorkerPool _workerPool;

        private DispatchThreadType _dispatchType;
        private int _threadId;
        private IParticipantReference<TRequest, TResponse> _funcReference;
        private int _maxRequests;
        private Func<TRequest, bool> _filter;
        private bool _useDedicatedThread;
        private Func<IParticipant<TRequest, TResponse>, IParticipant<TRequest, TResponse>> _modify;
        private ICircuitBreaker _circuitBreaker;
        private string _name;

        public ParticipantBuilder(IScatterGatherBus messageBus, IWorkerPool workerPool)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(workerPool, nameof(workerPool));

            _workerPool = workerPool;
            _dispatchType = DispatchThreadType.NoPreference;
        }

        public string Topic { get; private set; }

        public IParticipant<TRequest, TResponse> BuildParticipant()
        {
            if (_useDedicatedThread)
                _threadId = _workerPool.StartDedicatedWorker().ThreadId;

            if (_funcReference == null)
                throw new Exception("No actions defined");
            var participant = CreateParticipant(_funcReference, _dispatchType, _threadId, _name);

            participant = WrapParticipant(participant, _filter, _maxRequests);
            return participant;
        }

        public IDisposable WrapToken(IDisposable token)
        {
            Assert.ArgumentNotNull(token, nameof(token));

            if (_useDedicatedThread)
                return new SubscriptionWithDedicatedWorkerToken(_workerPool, token, _threadId);
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
            var reference = CreateReference(participant, useWeakReference);
            _funcReference = reference;
            return this;
        }

        public IThreadParticipantBuilder<TRequest, TResponse> InvokeEnvelope(Func<Envelope<TRequest>, TResponse> participant, bool useWeakReference = false)
        {
            Assert.ArgumentNotNull(participant, nameof(participant));
            ValidateDoesNotAlreadyHaveAction();
            var reference = CreateReference(participant, useWeakReference);
            _funcReference = reference;
            return this;
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

        public IDetailsParticipantBuilder<TRequest, TResponse> OnWorker()
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

        public IDetailsParticipantBuilder<TRequest, TResponse> OnDedicatedWorker()
        {
            ValidateDoesNotReadyHaveDispatchType();
            _dispatchType = DispatchThreadType.SpecificThread;
            _useDedicatedThread = true;
            return this;
        }

        public IDetailsParticipantBuilder<TRequest, TResponse> MaximumRequests(int maxRequests)
        {
            _maxRequests = maxRequests;
            return this;
        }

        public IDetailsParticipantBuilder<TRequest, TResponse> WithFilter(Func<TRequest, bool> filter)
        {
            if (filter == null)
                return this;
            if (_filter == null)
                _filter = filter;
            else
            {
                var oldFilter = _filter;
                _filter = r => oldFilter(r) && filter(r);
            }

            return this;
        }

        public IDetailsParticipantBuilder<TRequest, TResponse> ModifyParticipant(Func<IParticipant<TRequest, TResponse>, IParticipant<TRequest, TResponse>> modify)
        {
            Assert.ArgumentNotNull(modify, nameof(modify));
            if (_modify == null)
                _modify = modify;
            else
            {
                var oldModify = _modify;
                _modify = p => modify(oldModify(p));
            }
            return this;
        }

        public IDetailsParticipantBuilder<TRequest, TResponse> WithCircuitBreaker(int maxFailures, int breakMs)
        {
            ValidateDoesNotAlreadyHaveCircuitBreaker();
            _circuitBreaker = new SequentialCountingCircuitBreaker(breakMs, maxFailures);
            return this;
        }

        public IDetailsParticipantBuilder<TRequest, TResponse> WithCircuitBreaker(ICircuitBreaker circuitBreaker)
        {
            Assert.ArgumentNotNull(circuitBreaker, nameof(circuitBreaker));
            ValidateDoesNotAlreadyHaveCircuitBreaker();
            _circuitBreaker = circuitBreaker;
            return this;
        }

        public IDetailsParticipantBuilder<TRequest, TResponse> Named(string name)
        {
            _name = name;
            return this;
        }

        private void ValidateDoesNotAlreadyHaveCircuitBreaker()
        {
            if (_circuitBreaker != null)
                throw new Exception("Circuit breaker is already configured");
        }

        private void ValidateDoesNotAlreadyHaveAction()
        {
            if (_funcReference != null)
                throw new Exception("Builder already has an action defined");
        }

        private void ValidateDoesNotReadyHaveDispatchType()
        {
            if (_dispatchType != DispatchThreadType.NoPreference)
                throw new Exception($"Builder is already configured to use Dispatch Type {_dispatchType}");
        }

        private IParticipant<TRequest, TResponse> CreateParticipant(IParticipantReference<TRequest, TResponse> reference, DispatchThreadType dispatchType, int threadId, string name)
        {
            switch (dispatchType)
            {
                case DispatchThreadType.NoPreference:
                    return new AnyThreadParticipant<TRequest, TResponse>(reference, _workerPool, name);
                case DispatchThreadType.AnyWorkerThread:
                    return new AnyThreadParticipant<TRequest, TResponse>(reference, _workerPool, name);
                case DispatchThreadType.SpecificThread:
                    return new SpecificThreadParticipant<TRequest, TResponse>(reference, threadId, _workerPool, name);
                case DispatchThreadType.ThreadpoolThread:
                    return new ThreadPoolParticipant<TRequest, TResponse>(_workerPool, reference, name);
                case DispatchThreadType.Immediate:
                    return new ImmediateParticipant<TRequest, TResponse>(reference, name);
                default:
                    return new ImmediateParticipant<TRequest, TResponse>(reference, name);
            }
        }

        private IParticipant<TRequest, TResponse> WrapParticipant(IParticipant<TRequest, TResponse> participant, Func<TRequest, bool> filter, int maxRequests)
        {
            if (_circuitBreaker != null)
                participant = new CircuitBreakerParticipant<TRequest, TResponse>(participant, _circuitBreaker);
            if (filter != null)
                participant = new FilteredParticipant<TRequest, TResponse>(participant, filter);
            if (maxRequests > 0)
                participant = new MaxRequestsParticipant<TRequest, TResponse>(participant, maxRequests);
            if (_modify != null)
                participant = _modify(participant);
            return participant;
        }

        private static IParticipantReference<TRequest, TResponse> CreateReference(Func<TRequest, TResponse> participant, bool useWeakReference)
        {
            if (useWeakReference)
                return new WeakParticipantReference<TRequest, TResponse>(participant);
            return new StrongParticipantReference<TRequest, TResponse>(participant);
        }

        private static IParticipantReference<TRequest, TResponse> CreateReference(Func<Envelope<TRequest>, TResponse> participant, bool useWeakReference)
        {
            if (useWeakReference)
                return new EnvelopeWeakParticipantReference<TRequest, TResponse>(participant);
            return new EnvelopeStrongParticipantReference<TRequest, TResponse>(participant);
        }
    }
}
