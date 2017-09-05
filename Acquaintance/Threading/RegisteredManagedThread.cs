namespace Acquaintance.Threading
{
    public class RegisteredManagedThread
    {
        public RegisteredManagedThread(string owner, int threadId, string purpose)
        {
            Owner = owner;
            ThreadId = threadId;
            Purpose = purpose;
        }

        public string Owner { get; }
        public int ThreadId { get; }
        public string Purpose { get; }
    }
}