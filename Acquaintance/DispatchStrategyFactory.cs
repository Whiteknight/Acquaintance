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
            return new SimpleDispatchStrategy();
        }

        public IReqResChannelDispatchStrategy CreateRequestResponseStrategy()
        {
            return new RequestResponseChannelDispatchStrategy(true);
        }

        public IReqResChannelDispatchStrategy CreateScatterGatherStrategy()
        {
            return new RequestResponseChannelDispatchStrategy(false);
        }
    }

    public class TrieDispatchStrategyFactory : IDispatchStrategyFactory
    {
        public IPubSubChannelDispatchStrategy CreatePubSubStrategy()
        {
            return new TrieDispatchStrategy();
        }

        public IReqResChannelDispatchStrategy CreateRequestResponseStrategy()
        {
            return new RequestResponseChannelDispatchStrategy(true);
        }

        public IReqResChannelDispatchStrategy CreateScatterGatherStrategy()
        {
            return new RequestResponseChannelDispatchStrategy(false);
        }
    }
}
