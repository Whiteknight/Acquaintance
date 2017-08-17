using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Acquaintance.Logging;
using Acquaintance.Utility;

namespace Acquaintance.Threading
{
    public class MessagingWorkerThreadPool : IThreadPool, IDisposable
    {
        private readonly ILogger _log;
        private readonly int _maxQueuedMessages;
        private readonly List<MessageHandlerThread> _freeWorkers;
        private readonly IMessageHandlerThreadContext _freeWorkerContext;
        private readonly IActionDispatcher _threadPoolDispatcher;

        private readonly ConcurrentDictionary<int, IMessageHandlerThreadContext> _detachedContexts;
        private readonly ConcurrentDictionary<int, MessageHandlerThread> _dedicatedWorkers;
        private readonly ConcurrentDictionary<int, RegisteredManagedThread> _registeredThreads;

        public MessagingWorkerThreadPool(ILogger log, int numFreeWorkers = 0, int maxQueuedMessages = 1000)
        {
            Assert.IsInRange(maxQueuedMessages, nameof(maxQueuedMessages), 0, int.MaxValue);
            Assert.IsInRange(numFreeWorkers, nameof(numFreeWorkers), 0, 65535);

            _log = log;
            _maxQueuedMessages = maxQueuedMessages;
            _threadPoolDispatcher = new ThreadPoolActionDispatcher(_log);
            _freeWorkers = new List<MessageHandlerThread>();
            _dedicatedWorkers = new ConcurrentDictionary<int, MessageHandlerThread>();
            _detachedContexts = new ConcurrentDictionary<int, IMessageHandlerThreadContext>();
            _registeredThreads = new ConcurrentDictionary<int, RegisteredManagedThread>();
            _freeWorkerContext = InitializeFreeWorkers(numFreeWorkers);
        }

        public int NumberOfRunningFreeWorkers => _freeWorkers.Count;

        public ThreadToken StartDedicatedWorker()
        {
            var context = new MessageHandlerThreadContext(_maxQueuedMessages, _log);
            var worker = new MessageHandlerThread(context, "AcquaintanceDW");
            worker.Start();
            bool ok = _dedicatedWorkers.TryAdd(worker.ThreadId, worker);
            int threadId = ok ? worker.ThreadId : 0;
            return new ThreadToken(this, threadId);
        }

        public void StopDedicatedWorker(int threadId)
        {
            if (threadId <= 0)
                return;
            if (!_dedicatedWorkers.TryRemove(threadId, out MessageHandlerThread worker))
                return;
            worker.Stop();
            worker.Dispose();
        }

        public IActionDispatcher GetThreadDispatcher(int threadId, bool allowAutoCreate)
        {
            return GetThreadContext(threadId, allowAutoCreate);
        }

        private IMessageHandlerThreadContext GetThreadContext(int threadId, bool allowAutoCreate)
        {
            if (_freeWorkers.Any(t => t.ThreadId == threadId))
                return _freeWorkerContext;

            bool ok = _dedicatedWorkers.TryGetValue(threadId, out MessageHandlerThread worker);
            if (ok)
                return worker.Context;

            
            if (allowAutoCreate)
                return _detachedContexts.GetOrAdd(threadId, id => CreateDetachedContext());

            if (_detachedContexts.TryGetValue(threadId, out IMessageHandlerThreadContext context))
                return context;

            return new DummyMessageHandlerThreadContext(_log);
        }

        public IActionDispatcher GetFreeWorkerThreadDispatcher()
        {
            return _freeWorkerContext;
        }

        public IActionDispatcher GetThreadPoolActionDispatcher()
        {
            return _threadPoolDispatcher;
        }

        public IActionDispatcher GetAnyThreadDispatcher()
        {
            return GetFreeWorkerThreadDispatcher() ?? GetThreadPoolActionDispatcher();
        }

        public IActionDispatcher GetCurrentThreadDispatcher()
        {
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;
            return GetThreadDispatcher(currentThreadId, true);
        }

        public IMessageHandlerThreadContext GetCurrentThreadContext()
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
            private readonly IThreadPool _threadPool;
            private readonly int _threadId;

            public ManagedThreadToken(IThreadPool threadPool, int threadId)
            {
                _threadPool = threadPool;
                _threadId = threadId;
            }

            public void Dispose()
            {
                _threadPool.UnregisterManagedThread(_threadId);
            }
        }

        private IMessageHandlerThreadContext InitializeFreeWorkers(int numFreeWorkers)
        {
            if (numFreeWorkers <= 0)
                return null;

            var freeWorkerContext = new MessageHandlerThreadContext(_maxQueuedMessages, _log);
            for (int i = 0; i < numFreeWorkers; i++)
            {
                var thread = new MessageHandlerThread(freeWorkerContext, $"AcquaintanceFW{i}");
                _freeWorkers.Add(thread);
                thread.Start();
            }
            return freeWorkerContext;
        }

        private IMessageHandlerThreadContext CreateDetachedContext()
        {
            return new MessageHandlerThreadContext(_maxQueuedMessages, _log);
        }
    }
}