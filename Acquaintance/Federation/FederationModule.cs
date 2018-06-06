using System;
using System.Collections.Generic;
using System.Text;
using Acquaintance.Utility;

namespace Acquaintance.Federation
{
    public class FederationNode
    {
        public const string TopicAdded = "added";
        public const string TopicRemoved = "removed";

        public FederationNode(string id, string machineName, string communicationChannelName, IReadOnlyDictionary<string, string> communicationProperties)
        {
            Id = id;
            MachineName = machineName;
            CommunicationChannelName = communicationChannelName;
            CommunicationProperties = communicationProperties;
        }

        public string Id { get; }
        public string MachineName { get; }
        public string CommunicationChannelName { get; }
        public IReadOnlyDictionary<string, string> CommunicationProperties { get; }
    }

    public class FederationNodeRequest
    {

    }

    public static class FederationExtensions
    {

    }

    public interface IFederationBuilder
    {
        IFederationBuilder BroadcastIntervalS(int seconds);
    }

    public class FederationBuilder : IFederationBuilder
    {
        private int _broadcastIntervalSeconds;

        public void BuildTo(SubscriptionCollection messageBus)
        {

        }

        
        public IFederationBuilder BroadcastIntervalS(int seconds)
        {
            _broadcastIntervalSeconds = seconds;
            return this;
        }
    }

    public class FederationModule : IMessageBusModule
    {
        private readonly Action<IFederationBuilder> _setup;
        private readonly SubscriptionCollection _subscriptions;

        public FederationModule(IMessageBus messageBus, Action<IFederationBuilder> setup)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(setup, nameof(setup));

            _setup = setup;
            _subscriptions = new SubscriptionCollection(messageBus);
        }

        public void Start()
        {
            _subscriptions.Subscribe<FederationNode>(b => b
                .WithTopic(FederationNode.TopicAdded)
                .Invoke(AddNode));
            _subscriptions.Subscribe<FederationNode>(b => b
                .WithTopic(FederationNode.TopicRemoved)
                .Invoke(RemoveNode));
            _subscriptions.Listen<FederationNodeRequest, FederationNode>(b => b
                .WithDefaultTopic()
                .Invoke(GetNodeInfo));
            _subscriptions.Listen<FederationNodeRequest, FederationNode[]>(b => b
                .WithDefaultTopic()
                .Invoke(GetAllNodeInfo));

            var builder = new FederationBuilder();
            _setup(builder);
            builder.BuildTo(_subscriptions);
            
        }

        public void Stop()
        {
            _subscriptions.Clear();
        }

        private FederationNode[] GetAllNodeInfo(FederationNodeRequest arg)
        {
            throw new NotImplementedException();
        }

        private FederationNode GetNodeInfo(FederationNodeRequest arg)
        {
            throw new NotImplementedException();
        }

        private void RemoveNode(FederationNode obj)
        {
            throw new NotImplementedException();
        }

        private void AddNode(FederationNode obj)
        {
            throw new NotImplementedException();
        }
    }
}
