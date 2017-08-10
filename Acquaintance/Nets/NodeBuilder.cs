using Acquaintance.PubSub;
using System;
using System.Collections.Generic;
using System.Linq;
using Acquaintance.Utility;

namespace Acquaintance.Nets
{
    /// <summary>
    /// Builder type for building a Net Node. Behaviors and options of the Node can be specified
    /// in the builder.
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    public class NodeBuilder<TInput> : INodeBuilder
    {
        private readonly IMessageBus _messageBus;
        private readonly bool _readErrors;
        private readonly string _key;

        private Action<TInput> _action;
        private ISubscriptionHandler<TInput> _handler;
        private Func<TInput, bool> _predicate;
        private string _channelName;
        private int _onDedicatedThreads;

        // TODO: Maybe support some of the features from SubscriptionBuilder such as MaxEvents
        // and thread affinity. Review list of SubscriptionBuilder features for inclusion

        public NodeBuilder(string key, IMessageBus messageBus, bool readErrors)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));
            _messageBus = messageBus;
            _readErrors = readErrors;

            _key = key;
        }

        void INodeBuilder.BuildToMessageBus()
        {
            if (_action == null && _handler == null)
                throw new Exception("No action provided");

            if (_onDedicatedThreads == 0)
                SubscribeSingleWorker(_channelName, false);
            else if (_onDedicatedThreads == 1)
                SubscribeSingleWorker(_channelName, true);
            else
                SubscribeRoutedGroup();
        }

        public NodeBuilder<TInput> Transform<TOut>(Func<TInput, TOut> transform)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            if (_action != null)
                throw new Exception("Node already has a handler defined");

            string outputChannelName = OutputChannelName(_key);
            string errorChannelName = ErrorChannelName(_key);
            _action = m =>
            {
                try
                {
                    var result = transform(m);
                    var envelope = _messageBus.EnvelopeFactory.Create(outputChannelName, result);
                    _messageBus.PublishEnvelope(envelope);
                }
                catch (Exception e)
                {
                    var envelope = _messageBus.EnvelopeFactory.Create(errorChannelName, new NodeErrorMessage<TInput>(_key, m, e));
                    _messageBus.PublishEnvelope(envelope);
                }
            };
            return this;
        }

        public NodeBuilder<TInput> TransformMany<TOut>(Func<TInput, IEnumerable<TOut>> handler)
        {
            Assert.ArgumentNotNull(handler, nameof(handler));

            if (_action != null)
                throw new Exception("Node already has a handler defined");

            string outputChannelName = OutputChannelName(_key);
            string errorChannelName = ErrorChannelName(_key);
            _action = m =>
            {
                try
                {
                    var results = handler(m);
                    foreach (var r in results)
                        _messageBus.Publish(outputChannelName, r);
                }
                catch (Exception e)
                {
                    _messageBus.Publish(errorChannelName, new NodeErrorMessage<TInput>(_key, m, e));
                }
            };
            return this;
        }

        public NodeBuilder<TInput> Handle(Action<TInput> action)
        {
            Assert.ArgumentNotNull(action, nameof(action));
            if (_action != null || _handler != null)
                throw new Exception("Node already has a handler defined");

            string outputChannelName = OutputChannelName(_key);
            string errorChannelName = ErrorChannelName(_key);
            _action = m =>
            {
                try
                {
                    action(m);
                    _messageBus.Publish(outputChannelName, m);
                }
                catch (Exception e)
                {
                    _messageBus.Publish(errorChannelName, new NodeErrorMessage<TInput>(_key, m, e));
                }
            };
            return this;
        }

        public NodeBuilder<TInput> Handle(ISubscriptionHandler<TInput> handler)
        {
            Assert.ArgumentNotNull(handler, nameof(handler));
            if (_action != null || _handler != null)
                throw new Exception("Node already has a handler defined");
            string outputChannelName = OutputChannelName(_key);
            string errorChannelName = ErrorChannelName(_key);
            _handler = new NodeRepublishSubscriptionHandler<TInput>(handler, _messageBus, _key, outputChannelName, errorChannelName);
            return this;
        }

        public NodeBuilder<TInput> ReadInput()
        {
            if (!string.IsNullOrEmpty(_channelName))
                throw new Exception("Node can only read from a single input");
            if (_readErrors)
                throw new Exception("Cannot read errors from Net input");
            _channelName = Net.NetworkInputChannelName;
            return this;
        }

        public NodeBuilder<TInput> ReadOutputFrom(string nodeName)
        {
            Assert.ArgumentNotNull(nodeName, nameof(nodeName));
            if (!string.IsNullOrEmpty(_channelName))
                throw new Exception("Node can only read from a single input");

            string key = nodeName.ToLowerInvariant();
            string channelName = _readErrors ? ErrorChannelName(key) : OutputChannelName(key);
            _channelName = channelName;
            return this;
        }

        public NodeBuilder<TInput> OnCondition(Func<TInput, bool> predicate)
        {
            Assert.ArgumentNotNull(predicate, nameof(predicate));
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
            Assert.IsInRange(numThreads, nameof(numThreads), 1, 65535);

            _onDedicatedThreads = numThreads;
            return this;
        }

        private static string OutputChannelName(string key)
        {
            return "OutputFrom_" + key;
        }

        private static string ErrorChannelName(string key)
        {
            return "ErrorFrom_" + key;
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
    }
}
