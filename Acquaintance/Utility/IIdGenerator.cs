using System.Threading;

namespace Acquaintance.Utility
{
    public interface IIdGenerator
    {
        long GenerateNext();
    }

    public class LocalIncrementIdGenerator : IIdGenerator
    {
        private long _id;

        public LocalIncrementIdGenerator(long startId = 0)
        {
            Assert.IsInRange(startId, nameof(startId), 0, long.MaxValue);
            _id = startId;
        }

        public long GenerateNext()
        {
            return Interlocked.Increment(ref _id);
        }
    }
}
