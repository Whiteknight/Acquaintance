using System;
using System.Threading;

namespace Acquaintance.Threading
{
    public class MessageHandlerWorker : IDisposable
    {
        private readonly Thread _thread;
        private bool _started;

        public MessageHandlerWorker(IWorkerContext context, string name)
        {
            _thread = new Thread(HandlerThreadFunc)
            {
                Name = name
            };
            _started = false;
            Context = context;
        }

        public IWorkerContext Context { get; }

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
            var context = contextObject as IWorkerContext;
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
                    context.Log.Warn("Unhandled exception on worker thread: {1}\n{2}", e.Message, e.StackTrace);
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
