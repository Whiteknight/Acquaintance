using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Acquaintance.Timers;
using Acquaintance.Utility;

namespace Acquaintance.Federation
{
    public class FederationNode
    {
        public const string TopicBroadcast = "broadcast";
        public const string TopicAdded = "added";
        public const string TopicRemoved = "removed";

        public FederationNode(string id, string machineName, string communicationChannelName, IReadOnlyDictionary<string, string> communicationProperties)
        {
            Id = id;
            MachineName = machineName;
            CommunicationChannelName = communicationChannelName ?? "unknown";
            CommunicationProperties = communicationProperties ?? new Dictionary<string, string>();
        }

        public string Id { get; }
        public string MachineName { get; }
        public string CommunicationChannelName { get; }
        public IReadOnlyDictionary<string, string> CommunicationProperties { get; }
    }

    public class FederationNodeRequest
    {
        public string Id { get; set; }
        public string MachineName { get; set; }
        public string CommunicationChannelName { get; set; }

        public bool IsMatch(FederationNode node)
        {
            if (!string.IsNullOrEmpty(Id) && node.Id != Id)
                return false;

            if (!string.IsNullOrEmpty(MachineName) && node.MachineName != MachineName)
                return false;

            if (!string.IsNullOrEmpty(CommunicationChannelName) && node.CommunicationChannelName != CommunicationChannelName)
                return false;

            return true;
        }
    }

    public static class FederationExtensions
    {

    }

    public interface IFederationBuilder
    {
        IFederationBuilder OnTimerTick(string timerTopic, int multiple = 1);
        IFederationBuilder OnMessage<TPayload>(string topic);
    }

    public class FederationBuilder : IFederationBuilder
    {
        private readonly List<Action<SubscriptionCollection>> _onMessageBus;

        private readonly FederationModule _module;

        public FederationBuilder(FederationModule module)
        {
            _module = module;
            _onMessageBus = new List<Action<SubscriptionCollection>>();
        }

        public void BuildTo(SubscriptionCollection messageBus)
        {
            foreach (var act in _onMessageBus)
                act?.Invoke(messageBus);
        }

        public IFederationBuilder OnTimerTick(string timerTopic, int multiple = 1)
        {
            if (timerTopic == null && multiple <= 0)
                throw new Exception("Could not subscribe to timer with provided topic and multiple");
            _onMessageBus.Add(mb => mb.TimerSubscribe(timerTopic, multiple, b => b.Invoke(e => _module.BroadcastNodeInfo())));
            return this;
        }

        public IFederationBuilder OnMessage<TPayload>(string topic)
        {
            _onMessageBus.Add(mb => mb.Subscribe<TPayload>(b => b
                .WithTopic(topic)
                .Invoke(m => _module.BroadcastNodeInfo())));
            return this;
        }
    }

    public class FederationModule : IMessageBusModule
    {
        private readonly IMessageBus _messageBus;
        private readonly Action<IFederationBuilder> _setup;
        private readonly SubscriptionCollection _subscriptions;
        private readonly ConcurrentDictionary<string, FederationNode> _nodes;

        public FederationModule(IMessageBus messageBus, Action<IFederationBuilder> setup)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(setup, nameof(setup));

            _messageBus = messageBus;
            _setup = setup;
            _subscriptions = new SubscriptionCollection(messageBus);
            _nodes = new ConcurrentDictionary<string, FederationNode>();
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

            var builder = new FederationBuilder(this);
            _setup(builder);
            builder.BuildTo(_subscriptions);
        }

        public void Stop()
        {
            _subscriptions.Clear();
        }

        public void BroadcastNodeInfo()
        {
            _messageBus.Publish(FederationNode.TopicBroadcast, new FederationNode(_messageBus.Id, Environment.MachineName, "local", null));
        }

        private FederationNode[] GetAllNodeInfo(FederationNodeRequest request)
        {
            return _nodes.Values.Where(request.IsMatch).ToArray();
        }

        private FederationNode GetNodeInfo(FederationNodeRequest request)
        {
            return _nodes.Values.FirstOrDefault(request.IsMatch)
        }

        private void RemoveNode(FederationNode obj)
        {
            _nodes.TryRemove(obj.Id, out FederationNode node);
        }

        private void AddNode(FederationNode obj)
        {
            _nodes.TryAdd(obj.Id, obj);
        }
    }
}
