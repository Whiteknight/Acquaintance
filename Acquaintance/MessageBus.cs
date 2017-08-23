using Acquaintance.Common;
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
        private readonly SubscriptionDispatcher _subscriptionDispatcher;
        private readonly RequestDispatcher _requestDispatcher;
        private readonly ParticipantDispatcher _participantDispatcher;
        private readonly TopicRouter _router;

        public MessageBus(MessageBusCreateParameters parameters = null)
        {
            parameters = parameters ?? MessageBusCreateParameters.Default;
            var logger = parameters.GetLogger();
            ThreadPool = parameters.GetThreadPool(logger);

            Modules = new ModuleManager(this, logger);
            EnvelopeFactory = new EnvelopeFactory();

            _subscriptionDispatcher = new SubscriptionDispatcher(logger, parameters.AllowWildcards);
            _requestDispatcher = new RequestDispatcher(logger, parameters.AllowWildcards);
            _participantDispatcher = new ParticipantDispatcher(logger, parameters.AllowWildcards);
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
            _requestDispatcher.Request(topic, envelope, request);
            return request;
        }

        public IDisposable Listen<TRequest, TResponse>(string topic, IListener<TRequest, TResponse> listener)
        {
            Assert.ArgumentNotNull(listener, nameof(listener));
            return _requestDispatcher.Listen(topic, listener);
        }

        public IScatter<TResponse> ScatterEnvelope<TRequest, TResponse>(Envelope<TRequest> envelope)
        {
            var scatter = new Scatter<TResponse>();
            var topic = _router.RouteScatter<TRequest, TResponse>(envelope.Topic, envelope);
            _participantDispatcher.Scatter(topic ?? string.Empty, envelope, scatter);
            return scatter;
        }

        public IDisposable Participate<TRequest, TResponse>(string topic, IParticipant<TRequest, TResponse> participant)
        {
            return _participantDispatcher.Participate(topic ?? string.Empty, participant);
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
            _participantDispatcher.Dispose();

            (ThreadPool as IDisposable)?.Dispose();
            (Modules as IDisposable)?.Dispose();
        }
    }
}
