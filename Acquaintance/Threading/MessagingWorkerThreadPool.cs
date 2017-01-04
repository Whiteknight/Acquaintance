using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Acquaintance.Threading
{
    public class MessagingWorkerThreadPool : IThreadPool
    {
        private readonly List<MessageHandlerThread> _freeWorkers;
        private readonly ConcurrentDictionary<int, IMessageHandlerThreadContext> _detachedContexts;
        private readonly ConcurrentDictionary<int, MessageHandlerThread> _dedicatedWorkers;
        private int _currentThread;

        public MessagingWorkerThreadPool(int numFreeWorkers = 0)
        {
            _freeWorkers = new List<MessageHandlerThread>();

            _dedicatedWorkers = new ConcurrentDictionary<int, MessageHandlerThread>();
            _detachedContexts = new ConcurrentDictionary<int, IMessageHandlerThreadContext>();
            _currentThread = 0;

            if (numFreeWorkers < 0)
                throw new ArgumentOutOfRangeException(nameof(numFreeWorkers));
            for (int i = 0; i < numFreeWorkers; i++)
            {
                var context = new MessageHandlerThreadContext();
                var thread = new MessageHandlerThread(context);
                _freeWorkers.Add(thread);
                thread.Start();
            }
        }

        public int NumberOfRunningFreeWorkers => _freeWorkers.Count;

        public int StartDedicatedWorker()
        {
            var context = new MessageHandlerThreadContext();
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
            IMessageHandlerThreadContext context = _freeWorkers.Where(t => t.ThreadId == threadId).Select(t => t.Context).FirstOrDefault();
            if (context != null)
                return context;

            MessageHandlerThread worker;
            bool ok = _dedicatedWorkers.TryGetValue(threadId, out worker);
            if (ok)
                return worker.Context;

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
            if (_freeWorkers.Count == 0)
                return null;
            _currentThread = (_currentThread + 1) % _freeWorkers.Count;
            return _freeWorkers[_currentThread].Context;
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

        private IMessageHandlerThreadContext CreateDetachedContext()
        {
            return new MessageHandlerThreadContext();
        }

        public void Dispose()
        {
            foreach (var thread in _freeWorkers)
                thread.Stop();
            _freeWorkers.Clear();

            foreach (var thread in _dedicatedWorkers.Values)
                thread.Stop();

            foreach (var thread in _freeWorkers)
                thread.Dispose();
            _freeWorkers.Clear();

            foreach (var thread in _dedicatedWorkers.Values)
                thread.Dispose();
            _dedicatedWorkers.Clear();

            foreach (var context in _detachedContexts.Values)
                context.Dispose();
            _detachedContexts.Clear();
        }
    }
}