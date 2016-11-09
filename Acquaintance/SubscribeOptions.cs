using Acquaintance.Threading;

namespace Acquaintance
{
    public class SubscribeOptions
    {
        public SubscribeOptions()
        {
            ThreadId = 0;
            DispatchType = DispatchThreadType.NoPreference;
            KeepAlive = true;
        }

        public DispatchThreadType DispatchType { get; set; }
        public int ThreadId { get; set; }
        public int MaxEvents { get; set; }
        public bool KeepAlive { get; set; }

        public static SubscribeOptions Default => new SubscribeOptions();

        public static SubscribeOptions SpecificThread(int threadId)
        {
            return new SubscribeOptions
            {
                DispatchType = DispatchThreadType.SpecificThread,
                ThreadId = threadId
            };
        }
    }
}