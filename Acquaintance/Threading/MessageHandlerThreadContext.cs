using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Acquaintance.Threading
{
    public class MessageHandlerThreadContext : IDisposable
    {
        private readonly ConcurrentQueue<IThreadAction> _queue;
        private readonly ManualResetEventSlim _resetEvent;

        public MessageHandlerThreadContext()
        {
            _queue = new ConcurrentQueue<IThreadAction>();
            _resetEvent = new ManualResetEventSlim(false);
        }

        public bool ShouldStop { get; private set; }

        public void DispatchAction(IThreadAction action)
        {
            _queue.Enqueue(action);
            _resetEvent.Set();
        }

        public void Stop()
        {
            ShouldStop = true;
            _resetEvent.Set();
        }

        public void WaitForEvent(int? timeoutMs = null)
        {
            if (timeoutMs == null)
            {
                _resetEvent.Wait();
                _resetEvent.Reset();
                return;
            }

            if (timeoutMs.Value <= 0)
                return;
            bool isSet =_resetEvent.Wait(timeoutMs.Value);
            if (isSet)
                _resetEvent.Reset();
        }

        public IThreadAction GetAction()
        {
            IThreadAction action;
            bool hasValue = _queue.TryDequeue(out action);
            if (!hasValue || action == null)
                return null;
            return action;
        }

        public void Dispose()
        {
            _resetEvent.Dispose();
        }
    }
}