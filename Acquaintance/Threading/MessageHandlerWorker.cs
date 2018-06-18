using System;
using System.Threading;
using Acquaintance.Utility;

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
            if (!(contextObject is IWorkerContext context))
                return;

            while (true)
            {
                var action = context.GetAction();
                if (context.ShouldStop)
                    return;
                if (action == null)
                    continue;
                ErrorHandling.IgnoreExceptions(action.Execute, context.Log);
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
