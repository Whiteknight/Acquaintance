using Acquaintance.Modules;
using Acquaintance.PubSub;
using Acquaintance.RequestResponse;
using Acquaintance.ScatterGather;
using Acquaintance.Threading;
using System;
using System.Linq;
using Acquaintance.Logging;
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
            Id = parameters.Id ?? Guid.NewGuid();

            Logger = parameters.GetLogger() ?? new SilentLogger();

            WorkerPool = new WorkerPool(Logger, parameters.NumberOfWorkers, parameters.MaximumQueuedMessages);
            Modules = new ModuleManager(Logger);
            EnvelopeFactory = new EnvelopeFactory(Id);
            _subscriptionDispatcher = new SubscriptionDispatcher(Logger, parameters.AllowWildcards);
            _requestDispatcher = new RequestDispatcher(Logger, parameters.AllowWildcards);
            _participantDispatcher = new ParticipantDispatcher(Logger, parameters.AllowWildcards);
            _router = new TopicRouter();
        }

        public ILogger Logger { get; }
        public IModuleManager Modules { get; }
        public IWorkerPool WorkerPool { get; }
        public IPublishTopicRouter PublishRouter => _router;
        public IRequestTopicRouter RequestRouter => _router;
        public IScatterTopicRouter ScatterRouter => _router;

        public IEnvelopeFactory EnvelopeFactory { get; }

        public Guid Id { get; }

        public void PublishEnvelope<TPayload>(Envelope<TPayload> message)
        {
            var topics = _router.RoutePublish(message.Topics, message);
            _subscriptionDispatcher.Publish(topics, message);
        }

        public IDisposable Subscribe<TPayload>(string[] topics, ISubscription<TPayload> subscription)
        {
            Assert.ArgumentNotNull(subscription, nameof(subscription));

            subscription.Id = Guid.NewGuid();
            return _subscriptionDispatcher.Subscribe(topics, subscription);
        }

        public IRequest<TResponse> RequestEnvelope<TRequest, TResponse>(Envelope<TRequest> envelope)
        {
            var request = new Request<TResponse>();
            var topic = _router.RouteRequest<TRequest, TResponse>(envelope.Topics.FirstOrDefault(), envelope);
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
            var topic = _router.RouteScatter<TRequest, TResponse>(envelope.Topics.FirstOrDefault(), envelope);
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
            var threadContext = WorkerPool.GetCurrentThreadContext();
            while (!shouldStop() && !threadContext.ShouldStop)
            {
                var action = threadContext.GetAction(timeoutMs);
                action?.Execute();
            }
        }

        public void EmptyActionQueue(int max)
        {
            var threadContext = WorkerPool.GetCurrentThreadContext();
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

            (WorkerPool as IDisposable)?.Dispose();
            (Modules as IDisposable)?.Dispose();
        }
    }
}
