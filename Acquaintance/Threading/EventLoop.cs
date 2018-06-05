using System;
using System.Threading;

namespace Acquaintance.Threading
{
    /// <summary>
    /// Processes messages dispatched to a particular thread.
    /// </summary>
    public interface IEventLoop
    {
        /// <summary>
        /// Run an event loop until the condition is satisified or forever if no condition is provided
        /// </summary>
        /// <param name="shouldStop"></param>
        /// <param name="timeoutMs"></param>
        void Run(Func<bool> shouldStop = null, int timeoutMs = 500);

        /// <summary>
        /// Run an event loop until the cancellation token is set to the cancelled state
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="timeoutMs"></param>
        void Run(CancellationToken cancellationToken, int timeoutMs = 500);

        /// <summary>
        /// Processes all messages in the queue up to a given maximum. 
        /// This is used primarily to integrate Acquaintance message processing into an existing event loop or execution pipeline
        /// </summary>
        /// <param name="max"></param>
        void EmptyActionQueue(int max);
    }

    public class EventLoop : IEventLoop
    {
        private readonly IWorkerContext _workerContext;
        private readonly int _threadId;

        public EventLoop(IWorkerContext workerContext, int threadId)
        {
            _workerContext = workerContext;
            _threadId = threadId;
        }

        public static IEventLoop CreateEventLoopForTheCurrentThread(IWorkerPool workerPool)
        {
            var context = workerPool.GetCurrentThreadContext();
            return new EventLoop(context, Thread.CurrentThread.ManagedThreadId);
        }

        public void Run(Func<bool> shouldStop = null, int timeoutMs = 500)
        {
            AssertIsCorrectThread();
            if (shouldStop == null)
                shouldStop = () => false;
            while (!shouldStop() && !_workerContext.ShouldStop)
            {
                var action = _workerContext.GetAction(timeoutMs);
                action?.Execute();
            }
        }

        public void Run(CancellationToken cancellationToken, int timeoutMs = 500)
        {
            AssertIsCorrectThread();
            while (cancellationToken.IsCancellationRequested && !_workerContext.ShouldStop)
            {
                var action = _workerContext.GetAction(timeoutMs);
                action?.Execute();
            }
        }

        public void EmptyActionQueue(int max)
        {
            AssertIsCorrectThread();
            for (int i = 0; i < max; i++)
            {
                var action = _workerContext.GetAction();
                if (action == null)
                    break;
                action.Execute();
            }
        }

        private void AssertIsCorrectThread()
        {
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;
            if (currentThreadId != _threadId)
                throw new Exception($"Eventloop is not for this thread. Expected ThreadId={_threadId} but found {currentThreadId}. Please create a new runloop to execute on this thread");
        }
    }
}
