using System;
using System.Collections.Concurrent;
using System.Threading;
using Acquaintance.Logging;

namespace Acquaintance.Threading
{
    public class WorkerContext : IWorkerContext
    {
        private readonly BlockingCollection<IThreadAction> _queue;
        private int _disposing;
        private readonly int _maxQueuedMessages;

        public WorkerContext(int maxQueuedMessages, ILogger log)
        {
            Log = log ?? new SilentLogger();
            _queue = new BlockingCollection<IThreadAction>();
            _disposing = 0;
            _maxQueuedMessages = maxQueuedMessages;
        }

        public ILogger Log { get; }

        public bool ShouldStop { get; private set; }

        public void DispatchAction(IThreadAction action)
        {
            if (_queue.Count >= _maxQueuedMessages)
            {
                Log.Warn($"Action {action.GetType().Name} was not dispatched because of queue limits");
                return;
            }

            _queue.Add(action);
        }

        public void Stop()
        {
            ShouldStop = true;
            _queue.CompleteAdding();
        }

        public IThreadAction GetAction(int? timeoutMs = null)
        {
            try
            {
                if (!timeoutMs.HasValue || timeoutMs.Value > 0)
                    return _queue.Take();

                bool hasValue = _queue.TryTake(out IThreadAction action, timeoutMs.Value);
                return hasValue ? action : null;
            }
            catch (Exception e)
            {
                Log.Warn("Unhandled exception in thread context: {0}\n{1}", e.Message, e.StackTrace);
                return null;
            }
        }

        public void Dispose()
        {
            int disposing = Interlocked.CompareExchange(ref _disposing, 1, 0);
            if (disposing != 0)
                return;

            ShouldStop = true;
            _queue.Dispose();
        }
    }
}