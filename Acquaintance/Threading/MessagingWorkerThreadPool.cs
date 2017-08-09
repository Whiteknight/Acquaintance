using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Acquaintance.Logging;

namespace Acquaintance.Threading
{
    public class MessagingWorkerThreadPool : IThreadPool
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
            if (maxQueuedMessages <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxQueuedMessages));
            if (numFreeWorkers < 0)
                throw new ArgumentOutOfRangeException(nameof(numFreeWorkers));

            _log = log;
            _maxQueuedMessages = maxQueuedMessages;
            _threadPoolDispatcher = new ThreadPoolActionDispatcher(_log);
            _freeWorkers = new List<MessageHandlerThread>();
            _dedicatedWorkers = new ConcurrentDictionary<int, MessageHandlerThread>();
            _detachedContexts = new ConcurrentDictionary<int, IMessageHandlerThreadContext>();
            _registeredThreads = new ConcurrentDictionary<int, RegisteredManagedThread>();

            if (numFreeWorkers > 0)
            {
                _freeWorkerContext = new MessageHandlerThreadContext(_maxQueuedMessages, _log);
                for (int i = 0; i < numFreeWorkers; i++)
                {
                    var thread = new MessageHandlerThread(_freeWorkerContext, $"AcquaintanceFW{i}");
                    _freeWorkers.Add(thread);
                    thread.Start();
                }
            }
        }

        public int NumberOfRunningFreeWorkers => _freeWorkers.Count;

        public int StartDedicatedWorker()
        {
            var context = new MessageHandlerThreadContext(_maxQueuedMessages, _log);
            var worker = new MessageHandlerThread(context, "AcquaintanceDW");
            worker.Start();
            bool ok = _dedicatedWorkers.TryAdd(worker.ThreadId, worker);
            return ok ? worker.ThreadId : 0;
        }

        public void StopDedicatedWorker(int threadId)
        {
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

        public void RegisterManagedThread(IThreadManager manager, int threadId, string purpose)
        {
            var registration = new RegisteredManagedThread(manager, threadId, purpose);
            _registeredThreads.TryAdd(threadId, registration);
        }

        public void UnregisterManagedThread(int threadId)
        {
            _registeredThreads.TryRemove(threadId, out RegisteredManagedThread registration);
        }

        private IMessageHandlerThreadContext CreateDetachedContext()
        {
            return new MessageHandlerThreadContext(_maxQueuedMessages, _log);
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
    }
}