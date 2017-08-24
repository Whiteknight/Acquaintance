using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Acquaintance.Logging;
using Acquaintance.Utility;

namespace Acquaintance.Threading
{
    public class WorkerPool : IWorkerPool, IDisposable
    {
        private readonly ILogger _log;
        private readonly int _maxQueuedMessages;
        private readonly List<MessageHandlerWorker> _freeWorkers;
        private readonly IWorkerContext _freeWorkerContext;
        private readonly IActionDispatcher _threadPoolDispatcher;

        private readonly ConcurrentDictionary<int, IWorkerContext> _detachedContexts;
        private readonly ConcurrentDictionary<int, MessageHandlerWorker> _dedicatedWorkers;
        private readonly ConcurrentDictionary<int, RegisteredManagedThread> _registeredThreads;

        public WorkerPool(ILogger log, int numFreeWorkers = 0, int maxQueuedMessages = 1000)
        {
            Assert.IsInRange(maxQueuedMessages, nameof(maxQueuedMessages), 0, int.MaxValue);
            Assert.IsInRange(numFreeWorkers, nameof(numFreeWorkers), 0, 65535);

            _log = log;
            _maxQueuedMessages = maxQueuedMessages;
            _threadPoolDispatcher = new ThreadPoolDispatcher(_log);
            _freeWorkers = new List<MessageHandlerWorker>();
            _dedicatedWorkers = new ConcurrentDictionary<int, MessageHandlerWorker>();
            _detachedContexts = new ConcurrentDictionary<int, IWorkerContext>();
            _registeredThreads = new ConcurrentDictionary<int, RegisteredManagedThread>();
            _freeWorkerContext = InitializeFreeWorkers(numFreeWorkers);
        }

        public int NumberOfRunningFreeWorkers => _freeWorkers.Count;

        public WorkerToken StartDedicatedWorker()
        {
            var context = new WorkerContext(_maxQueuedMessages, _log);
            var worker = new MessageHandlerWorker(context, "AcquaintanceDW");
            worker.Start();
            bool ok = _dedicatedWorkers.TryAdd(worker.ThreadId, worker);
            int threadId = ok ? worker.ThreadId : 0;
            return new WorkerToken(this, threadId);
        }

        public void StopDedicatedWorker(int threadId)
        {
            if (threadId <= 0)
                return;
            if (!_dedicatedWorkers.TryRemove(threadId, out MessageHandlerWorker worker))
                return;
            worker.Stop();
            worker.Dispose();
        }

        public IActionDispatcher GetDispatcher(int threadId, bool allowAutoCreate)
        {
            return GetThreadContext(threadId, allowAutoCreate);
        }

        private IWorkerContext GetThreadContext(int threadId, bool allowAutoCreate)
        {
            if (_freeWorkers.Any(t => t.ThreadId == threadId))
                return _freeWorkerContext;

            bool ok = _dedicatedWorkers.TryGetValue(threadId, out MessageHandlerWorker worker);
            if (ok)
                return worker.Context;
            
            if (allowAutoCreate)
                return _detachedContexts.GetOrAdd(threadId, id => CreateDetachedContext());

            if (_detachedContexts.TryGetValue(threadId, out IWorkerContext context))
                return context;

            return new DummyWorkerContext(_log);
        }

        public IActionDispatcher GetFreeWorkerDispatcher()
        {
            return _freeWorkerContext;
        }

        public IActionDispatcher GetThreadPoolDispatcher()
        {
            return _threadPoolDispatcher;
        }

        public IActionDispatcher GetAnyWorkerDispatcher()
        {
            return GetFreeWorkerDispatcher() ?? GetThreadPoolDispatcher();
        }

        public IActionDispatcher GetCurrentThreadDispatcher()
        {
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;
            return GetDispatcher(currentThreadId, true);
        }

        public IWorkerContext GetCurrentThreadContext()
        {
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;
            return GetThreadContext(currentThreadId, true);
        }

        public IDisposable RegisterManagedThread(IThreadManager manager, int threadId, string purpose)
        {
            var registration = new RegisteredManagedThread(manager, threadId, purpose);
            _registeredThreads.TryAdd(threadId, registration);
            return new ManagedThreadToken(this, threadId);
        }

        public void UnregisterManagedThread(int threadId)
        {
            _registeredThreads.TryRemove(threadId, out RegisteredManagedThread registration);
        }

        public ThreadReport GetThreadReport()
        {
            var freeWorkers = _freeWorkers.Select(w => w.ThreadId).ToList();
            var dedicatedWorkers = _dedicatedWorkers.Values.Select(w => w.ThreadId).ToList();
            var registeredThreads = _registeredThreads.Values.ToList();
            return new ThreadReport(freeWorkers, dedicatedWorkers, registeredThreads);
        }

        public void Dispose()
        {
            // Cleanup the free workers and their contexts
            foreach (var thread in _freeWorkers)
                thread.Dispose();
            _freeWorkers.Clear();
            _freeWorkerContext.Dispose();

            // Cleanup dedicated workers and their contexts
            foreach (var thread in _dedicatedWorkers.Values)
            {
                thread.Stop();
                thread.Context.Dispose();
            }

            // Cleanup detached contexts for unmanaged threads.
            foreach (var context in _detachedContexts.Values)
                context.Dispose();
            _detachedContexts.Clear();
        }

        private class ManagedThreadToken : IDisposable
        {
            private readonly IWorkerPool _workerPool;
            private readonly int _threadId;

            public ManagedThreadToken(IWorkerPool workerPool, int threadId)
            {
                _workerPool = workerPool;
                _threadId = threadId;
            }

            public void Dispose()
            {
                _workerPool.UnregisterManagedThread(_threadId);
            }
        }

        private IWorkerContext InitializeFreeWorkers(int numFreeWorkers)
        {
            if (numFreeWorkers <= 0)
                return null;

            var freeWorkerContext = new WorkerContext(_maxQueuedMessages, _log);
            for (int i = 0; i < numFreeWorkers; i++)
            {
                var thread = new MessageHandlerWorker(freeWorkerContext, $"AcquaintanceFW{i}");
                _freeWorkers.Add(thread);
                thread.Start();
            }
            return freeWorkerContext;
        }

        private IWorkerContext CreateDetachedContext()
        {
            return new WorkerContext(_maxQueuedMessages, _log);
        }
    }
}