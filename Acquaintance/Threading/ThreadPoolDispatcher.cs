using System.Threading.Tasks;
using Acquaintance.Logging;
using Acquaintance.Utility;

namespace Acquaintance.Threading
{
    // Adaptor class from IActionDispatcher to Task.Factory.StartNew
    public class ThreadPoolDispatcher : IActionDispatcher
    {
        private readonly ILogger _log;

        public ThreadPoolDispatcher(ILogger log)
        {
            _log = log;
        }

        public void DispatchAction(IThreadAction action)
        {
            Task.Factory
                .StartNew(() => ErrorHandling.IgnoreExceptions(action.Execute, _log))
                .ConfigureAwait(false);
        }
    }
}
