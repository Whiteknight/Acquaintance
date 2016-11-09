using Acquaintance.Threading;

namespace Acquaintance
{
    public class ListenOptions
    {
        public ListenOptions()
        {
            WaitTimeoutMs = 5000;
            ThreadId = 0;
            DispatchType = DispatchThreadType.NoPreference;
            KeepAlive = true;
        }

        public DispatchThreadType DispatchType { get; set; }
        public int ThreadId { get; set; }
        public int WaitTimeoutMs { get; set; }
        public bool KeepAlive { get; set; }

        public static ListenOptions Default => new ListenOptions();

        public static ListenOptions SpecificThread(int threadId)
        {
            return new ListenOptions
            {
                DispatchType = DispatchThreadType.SpecificThread,
                ThreadId = threadId
            };
        }
    }
}