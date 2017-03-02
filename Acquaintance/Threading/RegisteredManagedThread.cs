namespace Acquaintance.Threading
{
    public class RegisteredManagedThread
    {
        public RegisteredManagedThread(IThreadManager manager, int threadId, string purpose)
        {
            Manager = manager;
            ThreadId = threadId;
            Purpose = purpose;
        }

        public IThreadManager Manager { get; }
        public int ThreadId { get; }
        public string Purpose { get; }
    }
}