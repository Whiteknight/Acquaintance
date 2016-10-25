using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Acquaintance.Threading
{
    public class MessagingWorkerThreadPool : IDisposable
    {
        private bool _started;
        private readonly List<MessageHandlerThread> _freeWorkers;
        private readonly ConcurrentDictionary<int, MessageHandlerThreadContext> _detachedContexts;
        private readonly ConcurrentDictionary<int, MessageHandlerThread> _dedicatedWorkers;
        private int _currentThread;

        public MessagingWorkerThreadPool()
        {
            _freeWorkers = new List<MessageHandlerThread>();
            
            _dedicatedWorkers = new ConcurrentDictionary<int, MessageHandlerThread>();
            _detachedContexts = new ConcurrentDictionary<int, MessageHandlerThreadContext>();
            _currentThread = 0;
            _started = false;
        }

        public void StartFreeWorkers(int numFreeWorkers)
        {
            if (_started)
                throw new Exception("Thread pool already started");
            _started = true;
            if (numFreeWorkers < 0)
                throw new ArgumentOutOfRangeException("numFreeWorkers");
            for (int i = 0; i < numFreeWorkers; i++)
            {
                var context = new MessageHandlerThreadContext();
                var thread = new MessageHandlerThread(context);
                _freeWorkers.Add(thread);
                thread.Start();
            }
        }

        public void StopFreeWorkers()
        {
            if (!_started)
                return;
            foreach (var thread in _freeWorkers)
                thread.Stop();
            _freeWorkers.Clear();
            
            _started = false;
        }


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

        public void StopAllDedicatedWorkers()
        {
            foreach (var thread in _dedicatedWorkers.Values)
                thread.Stop();
        }

        public MessageHandlerThreadContext GetThread(int threadId)
        {
            MessageHandlerThread worker;
            bool ok = _dedicatedWorkers.TryGetValue(threadId, out worker);
            if (ok)
                return worker.Context;
            return _freeWorkers.Where(t => t.ThreadId == threadId).Select(t => t.Context).FirstOrDefault();
        }

        public MessageHandlerThreadContext GetAnyThread()
        {
            if (_freeWorkers.Count == 0)
                return null;
            _currentThread = (_currentThread + 1) % _freeWorkers.Count;
            return _freeWorkers[_currentThread].Context;
        }

        public MessageHandlerThreadContext GetCurrentThread()
        {
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;
            var context = GetThread(currentThreadId);
            if (context != null)
                return context;

            context = _detachedContexts.GetOrAdd(currentThreadId, id => CreateDetachedContext());
            return context;
        }

        private MessageHandlerThreadContext CreateDetachedContext()
        {
            return new MessageHandlerThreadContext();
        }

        public void Dispose()
        {
            StopFreeWorkers();
            StopAllDedicatedWorkers();

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