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

    public class NetworkedIncrementIdGenerator : IIdGenerator
    {
        private readonly long _instanceId;
        private long _id;
        private const long MaxId = 0xFFFFFFFFFFFF;

        public NetworkedIncrementIdGenerator(ushort instanceId, long startId = 0)
        {
            Assert.IsInRange(startId, nameof(startId), 0, MaxId);
            _instanceId = ((long)instanceId) << 48;
            _id = startId;
        }

        public long GenerateNext()
        {
            var nextId = Interlocked.Increment(ref _id);
            return _instanceId | (nextId & MaxId);
        }
    }
}
