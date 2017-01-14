using Acquaintance.PubSub;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.Nets
{
    public class NodeBuilder<TInput> : INodeBuilder
    {
        private Action<TInput> _action;
        private ISubscriptionHandler<TInput> _handler;
        private Func<TInput, bool> _predicate;
        private string _channelName;
        private readonly IMessageBus _messageBus;
        private readonly string _key;
        private int _onDedicatedThreads;

        // TODO: Maybe support some of the features from SubscriptionBuilder such as MaxEvents
        // and thread affinity

        public NodeBuilder(string key, IMessageBus messageBus)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));
            _messageBus = messageBus;
            _key = key;
        }

        private static string OutputChannelName(string key)
        {
            return "OutputFrom_" + key;
        }

        void INodeBuilder.BuildToMessageBus()
        {
            if (_action == null)
                throw new Exception("No action provided");

            if (_onDedicatedThreads == 0)
                SubscribeSingleWorker(_channelName, false);
            else if (_onDedicatedThreads == 1)
                SubscribeSingleWorker(_channelName, true);
            else
                SubscribeRoutedGroup();
        }

        private void SubscribeRoutedGroup()
        {
            var routerOutputChannel = "Router_" + _key + "_";
            var workerChannels = Enumerable.Range(1, _onDedicatedThreads).Select(i => routerOutputChannel + i).ToArray();
            _messageBus.Subscribe<TInput>(b => b
                .WithChannelName(_channelName)
                .Distribute(workerChannels)
                .Immediate());
            for (int i = 0; i < _onDedicatedThreads; i++)
                SubscribeSingleWorker(workerChannels[i], true);
        }

        private void SubscribeSingleWorker(string channelName, bool useDedicatedThread)
        {
            _messageBus.Subscribe<TInput>(b1 =>
            {
                var b2 = string.IsNullOrEmpty(channelName) ? b1.OnDefaultChannel() : b1.WithChannelName(channelName);

                var b3 = _handler == null ? b2.Invoke(_action) : b2.Invoke(_handler);

                var b4 = useDedicatedThread ? b3.OnDedicatedThread() : b3.OnWorkerThread();

                if (_predicate != null)
                    b4.WithFilter(_predicate);
            });
        }

        public NodeBuilder<TInput> Transform<TOut>(Func<TInput, TOut> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (_action != null)
                throw new Exception("Node already has a handler defined");

            string outputChannelName = OutputChannelName(_key);
            _action = e =>
            {
                var result = handler(e);
                _messageBus.Publish(outputChannelName, result);
            };
            return this;
        }

        public NodeBuilder<TInput> TransformMany<TOut>(Func<TInput, IEnumerable<TOut>> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (_action != null)
                throw new Exception("Node already has a handler defined");

            string outputChannelName = OutputChannelName(_key);
            _action = e =>
            {
                var results = handler(e);
                foreach (var r in results)
                    _messageBus.Publish(outputChannelName, r);
            };
            return this;
        }

        public NodeBuilder<TInput> Handle(Action<TInput> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (_action != null || _handler != null)
                throw new Exception("Node already has a handler defined");
            _action = action;
            return this;
        }

        public NodeBuilder<TInput> Handle(ISubscriptionHandler<TInput> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (_action != null || _handler != null)
                throw new Exception("Node already has a handler defined");
            _handler = handler;
            return this;
        }



        public NodeBuilder<TInput> ReadInput()
        {
            if (!string.IsNullOrEmpty(_channelName))
                throw new Exception("Node can only read from a single input");
            _channelName = Net.NetworkInputChannelName;
            return this;
        }

        public NodeBuilder<TInput> ReadFrom(string nodeName)
        {
            if (string.IsNullOrEmpty(nodeName))
                throw new ArgumentNullException(nameof(nodeName));
            if (!string.IsNullOrEmpty(_channelName))
                throw new Exception("Node can only read from a single input");

            string key = nodeName.ToLowerInvariant();
            string outputChannelName = OutputChannelName(key);
            _channelName = outputChannelName;
            return this;
        }

        public NodeBuilder<TInput> OnCondition(Func<TInput, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            if (_predicate != null)
                throw new Exception("Node can only have a single predicate");
            _predicate = predicate;
            return this;
        }

        public NodeBuilder<TInput> OnDedicatedThread()
        {
            return OnDedicatedThreads(1);
        }

        public NodeBuilder<TInput> OnDedicatedThreads(int numThreads)
        {
            if (numThreads <= 0)
                throw new ArgumentOutOfRangeException(nameof(numThreads));

            _onDedicatedThreads = numThreads;
            return this;
        }
    }
}
