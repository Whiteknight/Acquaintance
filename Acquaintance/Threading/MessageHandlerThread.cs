using System;
using System.Threading;

namespace Acquaintance.Threading
{
    public class MessageHandlerThread : IDisposable
    {
        private readonly Thread _thread;
        private readonly MessageHandlerThreadContext _context;
        private bool _started;

        public MessageHandlerThread(MessageHandlerThreadContext context)
        {
            _thread = new Thread(HandlerThreadFunc);
            _started = false;
            _context = context;
        }

        public MessageHandlerThreadContext Context
        {
            get { return _context; }
        }

        public int ThreadId
        {
            get { return _thread.ManagedThreadId; }
        }

        public void Start()
        {
            if (_started)
                return;
            _thread.Start(_context);
            _started = true;
        }

        public void Stop()
        {
            if (!_started)
                return;
            _context.Stop();
            _thread.Join();
            _started = false;
        }

        public void DispatchAction(IThreadAction action)
        {
            _context.DispatchAction(action);
        }

        private static void HandlerThreadFunc(object contextObject)
        {
            MessageHandlerThreadContext context = contextObject as MessageHandlerThreadContext;
            if (context == null)
                return;

            while (true)
            {
                context.WaitForEvent();
                if (context.ShouldStop)
                    return;

                IThreadAction action = context.GetAction();
                while (action != null)
                {
                    try
                    {
                        action.Execute(context);
                    }
                    catch (Exception e)
                    {
                        // TODO: Log it or inform the user somehow?
                    }
                    action = context.GetAction();
                }
            }
        }

        public void Dispose()
        {
            Stop();
            _context.Dispose();
        }
    }
}
