using Acquaintance.Threading;

namespace Acquaintance
{
    public class SubscribeOptions
    {
        public SubscribeOptions()
        {
            WaitTimeoutMs = 5000;
            ThreadId = 0;
            DispatchType = DispatchThreadType.Immediate;
        }

        public DispatchThreadType DispatchType { get; set; }
        public int ThreadId { get; set; }
        public int WaitTimeoutMs { get; set; }

        public static SubscribeOptions Default
        {
            get { return new SubscribeOptions(); }
        }

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