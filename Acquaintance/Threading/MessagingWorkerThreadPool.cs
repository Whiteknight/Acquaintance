using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Acquaintance.Threading
{
    public class MessagingWorkerThreadPool : IThreadPool
    {
        private readonly int _maxQueuedMessages;
        private readonly List<MessageHandlerThread> _freeWorkers;
        private readonly IMessageHandlerThreadContext _freeWorkerContext;
        private readonly IActionDispatcher _threadPoolDispatcher;

        private readonly ConcurrentDictionary<int, IMessageHandlerThreadContext> _detachedContexts;
        private readonly ConcurrentDictionary<int, MessageHandlerThread> _dedicatedWorkers;
        private readonly ConcurrentDictionary<int, RegisteredManagedThread> _registeredThreads;

        public MessagingWorkerThreadPool(int numFreeWorkers = 0, int maxQueuedMessages = 1000)
        {
            if (maxQueuedMessages <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxQueuedMessages));
            _maxQueuedMessages = maxQueuedMessages;
            _threadPoolDispatcher = new ThreadPoolActionDispatcher();
            _freeWorkers = new List<MessageHandlerThread>();
            _dedicatedWorkers = new ConcurrentDictionary<int, MessageHandlerThread>();
            _detachedContexts = new ConcurrentDictionary<int, IMessageHandlerThreadContext>();
            _registeredThreads = new ConcurrentDictionary<int, RegisteredManagedThread>();

            if (numFreeWorkers < 0)
                throw new ArgumentOutOfRangeException(nameof(numFreeWorkers));
            if (numFreeWorkers > 0)
            {
                _freeWorkerContext = new MessageHandlerThreadContext(_maxQueuedMessages);
                for (int i = 0; i < numFreeWorkers; i++)
                {
                    var thread = new MessageHandlerThread(_freeWorkerContext);
                    _freeWorkers.Add(thread);
                    thread.Start();
                }
            }
        }

        public int NumberOfRunningFreeWorkers => _freeWorkers.Count;

        public int StartDedicatedWorker()
        {
            var context = new MessageHandlerThreadContext(_maxQueuedMessages);
            var worker = new MessageHandlerThread(context);
            worker.Start();
            bool ok = _dedicatedWorkers.TryAdd(worker.ThreadId, worker);
            if (!ok)
                return 0;
            return worker.ThreadId;
        }

        public void StopDedicatedWorker(int threadId)
        {
            MessageHandlerThread worker;
            bool ok = _dedicatedWorkers.TryRemove(threadId, out worker);
            if (ok)
            {
                worker.Stop();
                worker.Dispose();
            }
        }

        public IActionDispatcher GetThreadDispatcher(int threadId, bool allowAutoCreate)
        {
            return GetThreadContext(threadId, allowAutoCreate);
        }

        private IMessageHandlerThreadContext GetThreadContext(int threadId, bool allowAutoCreate)
        {
            if (_freeWorkers.Any(t => t.ThreadId == threadId))
                return _freeWorkerContext;

            MessageHandlerThread worker;
            bool ok = _dedicatedWorkers.TryGetValue(threadId, out worker);
            if (ok)
                return worker.Context;

            IMessageHandlerThreadContext context;
            if (allowAutoCreate)
            {
                context = _detachedContexts.GetOrAdd(threadId, id => CreateDetachedContext());
                return context;
            }

            ok = _detachedContexts.TryGetValue(threadId, out context);
            if (ok)
                return context;

            return new DummyMessageHandlerThreadContext();
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
            RegisteredManagedThread registration;
            _registeredThreads.TryRemove(threadId, out registration);
        }

        private IMessageHandlerThreadContext CreateDetachedContext()
        {
            return new MessageHandlerThreadContext(_maxQueuedMessages);
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