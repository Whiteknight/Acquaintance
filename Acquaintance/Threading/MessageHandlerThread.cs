using System;
using System.Threading;

namespace Acquaintance.Threading
{
    public class MessageHandlerThread : IDisposable
    {
        private readonly Thread _thread;
        private bool _started;

        public MessageHandlerThread(IMessageHandlerThreadContext context, string name)
        {
            _thread = new Thread(HandlerThreadFunc);
            _thread.Name = name;
            _started = false;
            Context = context;
        }

        public IMessageHandlerThreadContext Context { get; }

        public int ThreadId => _thread.ManagedThreadId;

        public void Start()
        {
            if (_started)
                return;
            _thread.Start(Context);
            _started = true;
        }

        public void Stop()
        {
            if (!_started)
                return;
            Context.Stop();
            _thread.Join();
            _started = false;
        }

        private static void HandlerThreadFunc(object contextObject)
        {
            var context = contextObject as IMessageHandlerThreadContext;
            if (context == null)
                return;

            while (true)
            {
                IThreadAction action = context.GetAction();
                if (context.ShouldStop)
                    return;
                if (action == null)
                    continue;
                try
                {
                    action.Execute();
                }
                catch (Exception e)
                {
                    // TODO: Log it or inform the user somehow?
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
