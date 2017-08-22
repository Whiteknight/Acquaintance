using Acquaintance.RequestResponse;
using Acquaintance.ScatterGather;

namespace Acquaintance
{
    public interface IDispatchStrategyFactory
    {
        IScatterGatherChannelDispatchStrategy CreateScatterGatherStrategy();
    }

    public class SimpleDispatchStrategyFactory : IDispatchStrategyFactory
    {
        public IScatterGatherChannelDispatchStrategy CreateScatterGatherStrategy()
        {
            return new ScatterGatherSimpleDispatchStrategy();
        }
    }

    public class TrieDispatchStrategyFactory : IDispatchStrategyFactory
    {
        public IScatterGatherChannelDispatchStrategy CreateScatterGatherStrategy()
        {
            return new ScatterGatherTrieDispatchStrategy();
        }
    }
}
