using System;
using System.Threading.Tasks;
using Acquaintance.Logging;

namespace Acquaintance.Threading
{
    public class ThreadPoolActionDispatcher : IActionDispatcher
    {
        private readonly ILogger _log;

        public ThreadPoolActionDispatcher(ILogger log)
        {
            _log = log;
        }

        public void DispatchAction(IThreadAction action)
        {
            Task.Factory
                .StartNew(() =>
                {
                    try
                    {
                        action.Execute();
                    }
                    catch (Exception e)
                    {
                        _log.Warn("Unhandled exception in threadpool dispatcher: {0}\n{1}", e.Message, e.StackTrace);
                    }
                })
                .ConfigureAwait(false);
        }
    }
}
