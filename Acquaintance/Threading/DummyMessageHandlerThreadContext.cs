namespace Acquaintance.Threading
{
    public class DummyMessageHandlerThreadContext : IMessageHandlerThreadContext
    {
        public void Dispose()
        {
        }

        public bool ShouldStop => true;

        public void DispatchAction(IThreadAction action)
        {
        }

        public void Stop()
        {
        }

        public void WaitForEvent(int? timeoutMs = null)
        {
        }

        public IThreadAction GetAction()
        {
            return null;
        }
    }
}