using Acquaintance.Logging;

namespace Acquaintance.Threading
{
    public class DummyMessageHandlerThreadContext : IMessageHandlerThreadContext
    {
        public DummyMessageHandlerThreadContext(ILogger log)
        {
            Log = log;
        }

        public ILogger Log { get; }
        public bool ShouldStop => true;

        public void DispatchAction(IThreadAction action)
        {
        }

        public void Stop()
        {
        }

        public IThreadAction GetAction(int? timeoutMs = null)
        {
            return null;
        }

        public void Dispose()
        {
        }
    }
}