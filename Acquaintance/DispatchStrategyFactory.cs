using Acquaintance.PubSub;
using Acquaintance.RequestResponse;

namespace Acquaintance
{
    public interface IDispatchStrategyFactory
    {
        IPubSubChannelDispatchStrategy CreatePubSubStrategy();

        IReqResChannelDispatchStrategy CreateRequestResponseStrategy();
        IReqResChannelDispatchStrategy CreateScatterGatherStrategy();
    }

    public class SimpleDispatchStrategyFactory : IDispatchStrategyFactory
    {
        public IPubSubChannelDispatchStrategy CreatePubSubStrategy()
        {
            return new PubSubSimpleDispatchStrategy();
        }

        public IReqResChannelDispatchStrategy CreateRequestResponseStrategy()
        {
            return new ReqResSimpleDispatchStrategy(true);
        }

        public IReqResChannelDispatchStrategy CreateScatterGatherStrategy()
        {
            return new ReqResSimpleDispatchStrategy(false);
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
            return new ReqResTrieDispatchStrategy(true);
        }

        public IReqResChannelDispatchStrategy CreateScatterGatherStrategy()
        {
            return new ReqResTrieDispatchStrategy(false);
        }
    }
}
