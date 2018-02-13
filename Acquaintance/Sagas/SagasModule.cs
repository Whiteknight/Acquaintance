using System;
using System.Collections.Concurrent;
using System.Threading;
using Acquaintance.Threading;
using Acquaintance.Utility;

namespace Acquaintance.Sagas
{
    public class SagasModule : IMessageBusModule, IDisposable
    {
        private readonly int _numberOfThreads;
        private readonly ConcurrentDictionary<Guid, IDisposable> _sagas;
        private readonly WorkerToken[] _threadTokens;

        private readonly IMessageBus _messageBus;
        private int _currentThreadIndex;

        public SagasModule(IMessageBus messageBus, int numberOfThreads)
        {
            Assert.IsInRange(numberOfThreads, nameof(numberOfThreads), 1, 50);
            _messageBus = messageBus;
            _numberOfThreads = numberOfThreads;
            _threadTokens = new WorkerToken[numberOfThreads];
            _sagas = new ConcurrentDictionary<Guid, IDisposable>();
            _currentThreadIndex = 0;
        }

        public void Start()
        {
            for (int i = 0; i < _numberOfThreads; i++)
                _threadTokens[i] = _messageBus.WorkerPool.StartDedicatedWorker();
        }

        public void Stop()
        {
            foreach (var saga in _sagas.Values)
                saga.Dispose();
            _sagas.Clear();

            for (int i = 0; i < _numberOfThreads; i++)
            {
                _threadTokens[i].Dispose();
                _threadTokens[i] = null;
            }
        }

        public Saga<TState, TKey> CreateSaga<TState, TKey>()
        {
            int threadId = GetNextThreadId();
            var repository = new SagaRepository<TState, TKey>();
            return new Saga<TState, TKey>(_messageBus, repository, threadId);
        }

        public IDisposable AddSaga(IDisposable saga)
        {
            var id = Guid.NewGuid();
            _sagas.TryAdd(id, saga);
            return new SagaToken(this, id);
        }

        private int GetNextThreadId()
        {
            var threadIndex = Interlocked.Increment(ref _currentThreadIndex);
            return _threadTokens[threadIndex % _numberOfThreads].ThreadId;
        }

        public void Dispose()
        {
            Stop();
        }

        private void RemoveSaga(Guid id)
        {
            bool ok = _sagas.TryRemove(id, out IDisposable saga);
            if (ok)
                saga?.Dispose();
        }

        private class SagaToken : IDisposable
        {
            private readonly SagasModule _module;
            private readonly Guid _id;

            public SagaToken(SagasModule module, Guid id)
            {
                _module = module;
                _id = id;
            }

            public void Dispose()
            {
                _module.RemoveSaga(_id);
            }
        }
    }
}