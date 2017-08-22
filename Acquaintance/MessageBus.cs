using Acquaintance.Common;
using Acquaintance.Logging;
using Acquaintance.Modules;
using Acquaintance.PubSub;
using Acquaintance.RequestResponse;
using Acquaintance.ScatterGather;
using Acquaintance.Threading;
using System;
using Acquaintance.Routing;
using Acquaintance.Utility;

namespace Acquaintance
{
    /// <summary>
    /// The message bus object, which coordinates communication features.
    /// </summary>
    public sealed class MessageBus : IMessageBus
    {
        private readonly ILogger _logger;
        private readonly SubscriptionDispatcher _subscriptionDispatcher;
        private readonly RequestDispatcher _requestDispatcher;
        private readonly IScatterGatherChannelDispatchStrategy _scatterGatherStrategy;
        private readonly TopicRouter _router;

        public MessageBus(MessageBusCreateParameters parameters = null)
        {
            parameters = parameters ?? MessageBusCreateParameters.Default;
            _logger = parameters.GetLogger();
            ThreadPool = parameters.GetThreadPool(_logger);

            Modules = new ModuleManager(this, _logger);
            EnvelopeFactory = new EnvelopeFactory();

            var dispatcherFactory = parameters.GetDispatchStrategyFactory();
            _subscriptionDispatcher = new SubscriptionDispatcher(_logger, parameters.AllowWildcards);
            _requestDispatcher = new RequestDispatcher(_logger, parameters.AllowWildcards);
            _scatterGatherStrategy = dispatcherFactory.CreateScatterGatherStrategy();
            _router = new TopicRouter();
        }

        public IModuleManager Modules { get; }
        public IThreadPool ThreadPool { get; }
        public IPublishTopicRouter PublishRouter => _router;
        public IRequestTopicRouter RequestRouter => _router;
        public IScatterTopicRouter ScatterRouter => _router;

        public IEnvelopeFactory EnvelopeFactory { get; }

        public void PublishEnvelope<TPayload>(Envelope<TPayload> message)
        {
            var topics = _router.RoutePublish(message.Topic, message);
            _subscriptionDispatcher.Publish(topics, message);
        }

        public IDisposable Subscribe<TPayload>(string topic, ISubscription<TPayload> subscription)
        {
            Assert.ArgumentNotNull(subscription, nameof(subscription));

            subscription.Id = Guid.NewGuid();
            return _subscriptionDispatcher.Subscribe(topic, subscription);
        }

        public IRequest<TResponse> RequestEnvelope<TRequest, TResponse>(Envelope<TRequest> envelope)
        {
            var request = new Request<TResponse>();
            var topic = _router.RouteRequest<TRequest, TResponse>(envelope.Topic, envelope);
            _requestDispatcher.Request<TRequest, TResponse>(topic, envelope, request);
            return request;
        }

        public IDisposable Listen<TRequest, TResponse>(string topic, IListener<TRequest, TResponse> listener)
        {
            Assert.ArgumentNotNull(listener, nameof(listener));
            return _requestDispatcher.Listen<TRequest, TResponse>(topic, listener);
        }

        public IScatter<TResponse> ScatterEnvelope<TRequest, TResponse>(string topic, Envelope<TRequest> envelope)
        {
            var scatter = new Scatter<TResponse>();
            topic = _router.RouteScatter<TRequest, TResponse>(topic, envelope);
            if (topic == null)
                return scatter;
            foreach (var channel in _scatterGatherStrategy.GetExistingChannels<TRequest, TResponse>(topic))
            {
                _logger.Debug("Requesting RequestType={0} ResponseType={1} Topic={2} to channel Id={3}", typeof(TRequest).FullName, typeof(TResponse).FullName, topic, channel.Id);
                channel.Scatter(envelope, scatter);
            }

            return scatter;
        }

        public IDisposable Participate<TRequest, TResponse>(string topic, IParticipant<TRequest, TResponse> participant)
        {
            Assert.ArgumentNotNull(participant, nameof(participant));

            participant.Id = Guid.NewGuid();
            var channel = _scatterGatherStrategy.GetChannelForSubscription<TRequest, TResponse>(topic, _logger);
            _logger.Debug("Participant {0} on RequestType={1} ResponseType={2} Topic={3}, on channel Id={4}", participant.Id, typeof(TRequest).FullName, typeof(TResponse).FullName, topic, channel.Id);
            return channel.Participate(participant);
        }

        public void RunEventLoop(Func<bool> shouldStop = null, int timeoutMs = 500)
        {
            if (shouldStop == null)
                shouldStop = () => false;
            var threadContext = ThreadPool.GetCurrentThreadContext();
            while (!shouldStop() && !threadContext.ShouldStop)
            {
                var action = threadContext.GetAction(timeoutMs);
                action?.Execute();
            }
        }

        public void EmptyActionQueue(int max)
        {
            var threadContext = ThreadPool.GetCurrentThreadContext();
            for (int i = 0; i < max; i++)
            {
                var action = threadContext.GetAction();
                if (action == null)
                    break;
                action.Execute();
            }
        }

        public void Dispose()
        {
            _subscriptionDispatcher.Dispose();
            _requestDispatcher.Dispose();
            _scatterGatherStrategy.Dispose();

            (ThreadPool as IDisposable)?.Dispose();
            (Modules as IDisposable)?.Dispose();
        }
    }
}
