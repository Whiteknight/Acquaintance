using Acquaintance.RequestResponse;
using Acquaintance.ScatterGather;

namespace Acquaintance
{
    public interface IDispatchStrategyFactory
    {
        bool AllowWildcards { get; }

        IReqResChannelDispatchStrategy CreateRequestResponseStrategy();
        IScatterGatherChannelDispatchStrategy CreateScatterGatherStrategy();
    }

    public class SimpleDispatchStrategyFactory : IDispatchStrategyFactory
    {
        public bool AllowWildcards => false;

        public IReqResChannelDispatchStrategy CreateRequestResponseStrategy()
        {
            return new ReqResSimpleDispatchStrategy();
        }

        public IScatterGatherChannelDispatchStrategy CreateScatterGatherStrategy()
        {
            return new ScatterGatherSimpleDispatchStrategy();
        }
    }

    public class TrieDispatchStrategyFactory : IDispatchStrategyFactory
    {
        public bool AllowWildcards => true;

        public IReqResChannelDispatchStrategy CreateRequestResponseStrategy()
        {
            return new ReqResTrieDispatchStrategy();
        }

        public IScatterGatherChannelDispatchStrategy CreateScatterGatherStrategy()
        {
            return new ScatterGatherTrieDispatchStrategy();
        }
    }
}
