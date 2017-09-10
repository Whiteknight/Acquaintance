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
        private string _topic;
        private int _onDedicatedThreads;

        // TODO: Maybe support some of the features from SubscriptionBuilder such as MaxEvents
        // and thread affinity. Review list of SubscriptionBuilder features for inclusion
        // TODO: Interface segregation to avoid duplicate/conflicting method calls

        public NodeBuilder(string key, IMessageBus messageBus, bool readErrors)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            _messageBus = messageBus;
            _readErrors = readErrors;

            _key = key;
        }

        void INodeBuilder.BuildToMessageBus()
        {
            if (_action == null && _handler == null)
                throw new Exception("No action provided");

            if (_onDedicatedThreads == 0)
                SubscribeSingleWorker(_topic, false);
            else if (_onDedicatedThreads == 1)
                SubscribeSingleWorker(_topic, true);
            else
                SubscribeRoutedGroup();
        }

        public NodeBuilder<TInput> Transform<TOut>(Func<TInput, TOut> transform)
        {
            Assert.ArgumentNotNull(transform, nameof(transform));
            ValidateDoesNotAlreadyHaveAction();

            string outputTopic = OutputTopic(_key);
            string errorTopic = ErrorTopic(_key);
            _action = m =>
            {
                try
                {
                    var result = transform(m);
                    var envelope = _messageBus.EnvelopeFactory.Create(outputTopic, result);
                    _messageBus.PublishEnvelope(envelope);
                }
                catch (Exception e)
                {
                    var envelope = _messageBus.EnvelopeFactory.Create(errorTopic, new NodeErrorMessage<TInput>(_key, m, e));
                    _messageBus.PublishEnvelope(envelope);
                }
            };
            return this;
        }

        public NodeBuilder<TInput> TransformMany<TOut>(Func<TInput, IEnumerable<TOut>> handler)
        {
            Assert.ArgumentNotNull(handler, nameof(handler));
            ValidateDoesNotAlreadyHaveAction();

            string outputTopic = OutputTopic(_key);
            string errorTopic = ErrorTopic(_key);
            _action = m =>
            {
                try
                {
                    var results = handler(m);
                    foreach (var r in results)
                        _messageBus.Publish(outputTopic, r);
                }
                catch (Exception e)
                {
                    _messageBus.Publish(errorTopic, new NodeErrorMessage<TInput>(_key, m, e));
                }
            };
            return this;
        }

        public NodeBuilder<TInput> Handle(Action<TInput> action)
        {
            Assert.ArgumentNotNull(action, nameof(action));
            ValidateDoesNotAlreadyHaveAction();

            string outputTopic = OutputTopic(_key);
            string errorTopic = ErrorTopic(_key);
            _action = m =>
            {
                try
                {
                    action(m);
                    _messageBus.Publish(outputTopic, m);
                }
                catch (Exception e)
                {
                    _messageBus.Publish(errorTopic, new NodeErrorMessage<TInput>(_key, m, e));
                }
            };
            return this;
        }

        public NodeBuilder<TInput> Handle(ISubscriptionHandler<TInput> handler)
        {
            Assert.ArgumentNotNull(handler, nameof(handler));
            ValidateDoesNotAlreadyHaveAction();
            string outputTopic = OutputTopic(_key);
            string errorTopic = ErrorTopic(_key);
            _handler = new NodeRepublishSubscriptionHandler<TInput>(handler, _messageBus, _key, outputTopic, errorTopic);
            return this;
        }

        public NodeBuilder<TInput> ReadInput()
        {
            ValidateDoesNotAlreadyHaveTopic();
            if (_readErrors)
                throw new Exception("Cannot read errors from Net input");
            _topic = Net.NetworkInputTopic;
            return this;
        }

        public NodeBuilder<TInput> ReadOutputFrom(string nodeName)
        {
            Assert.ArgumentNotNull(nodeName, nameof(nodeName));
            ValidateDoesNotAlreadyHaveTopic();

            string key = nodeName.ToLowerInvariant();
            _topic = _readErrors ? ErrorTopic(key) : OutputTopic(key);
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

        private void ValidateDoesNotAlreadyHaveAction()
        {
            if (_action != null || _handler != null)
                throw new Exception("Node already has a handler defined");
        }

        private void ValidateDoesNotAlreadyHaveTopic()
        {
            if (!string.IsNullOrEmpty(_topic))
                throw new Exception("Node can only read from a single input");
        }

        private static string OutputTopic(string key)
        {
            return "OutputFrom_" + key;
        }

        private static string ErrorTopic(string key)
        {
            return "ErrorFrom_" + key;
        }

        private void SubscribeRoutedGroup()
        {
            var routerOutputTopic = "Router_" + _key + "_";
            var workerTopics = Enumerable.Range(1, _onDedicatedThreads).Select(i => routerOutputTopic + i).ToArray();
            _messageBus.SetupPublishDistribution<TInput>(_topic, workerTopics);
            for (int i = 0; i < _onDedicatedThreads; i++)
                SubscribeSingleWorker(workerTopics[i], true);
        }

        private void SubscribeSingleWorker(string topic, bool useDedicatedThread)
        {
            _messageBus.Subscribe<TInput>(b1 =>
            {
                var b2 = string.IsNullOrEmpty(topic) ? b1.WithDefaultTopic() : b1.WithTopic(topic);

                var b3 = _handler == null ? b2.Invoke(_action) : b2.Invoke(_handler);

                var b4 = useDedicatedThread ? b3.OnDedicatedWorker() : b3.OnWorker();

                if (_predicate != null)
                    b4.WithFilter(_predicate);
            });
        }
    }
}
