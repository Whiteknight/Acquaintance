using System.Collections.Concurrent;
using System.Threading;

namespace Acquaintance.Threading
{
    public class MessageHandlerThreadContext : IMessageHandlerThreadContext
    {
        private readonly BlockingCollection<IThreadAction> _queue;
        private int _disposing;

        public MessageHandlerThreadContext()
        {
            _queue = new BlockingCollection<IThreadAction>();
            _disposing = 0;
        }

        public bool ShouldStop { get; private set; }

        public void DispatchAction(IThreadAction action)
        {
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

                IThreadAction action;
                bool hasValue = _queue.TryTake(out action, timeoutMs.Value);
                if (!hasValue || action == null)
                    return null;
                return action;
            }
            catch
            {
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