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

        public IThreadAction GetAction(int? timeoutMs = null)
        {
            return null;
        }
    }
}