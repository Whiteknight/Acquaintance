using Acquaintance.PubSub;
using Acquaintance.RequestResponse;
using Acquaintance.ScatterGather;

namespace Acquaintance
{
    public interface IDispatchStrategyFactory
    {
        IPubSubChannelDispatchStrategy CreatePubSubStrategy();

        IReqResChannelDispatchStrategy CreateRequestResponseStrategy();
        IScatterGatherChannelDispatchStrategy CreateScatterGatherStrategy();
    }

    public class SimpleDispatchStrategyFactory : IDispatchStrategyFactory
    {
        public IPubSubChannelDispatchStrategy CreatePubSubStrategy()
        {
            return new PubSubSimpleDispatchStrategy();
        }

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
        public IPubSubChannelDispatchStrategy CreatePubSubStrategy()
        {
            return new PubSubTrieDispatchStrategy();
        }

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
