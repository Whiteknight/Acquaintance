using Acquaintance.Common;
using Acquaintance.Logging;
using Acquaintance.Modules;
using Acquaintance.PubSub;
using Acquaintance.RequestResponse;
using Acquaintance.ScatterGather;
using Acquaintance.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using Acquaintance.Utility;

namespace Acquaintance
{
    /// <summary>
    /// The message bus object, which coordinates communication features.
    /// </summary>
    public sealed class MessageBus : IMessageBus
    {
        private readonly ILogger _logger;
        private readonly IPubSubChannelDispatchStrategy _pubSubStrategy;
        private readonly IPubSubChannelDispatchStrategy _eavesdropStrategy;
        private readonly IReqResChannelDispatchStrategy _requestResponseStrategy;
        private readonly IScatterGatherChannelDispatchStrategy _scatterGatherStrategy;

        public MessageBus(MessageBusCreateParameters parameters = null)
        {
            parameters = parameters ?? MessageBusCreateParameters.Default;
            _logger = parameters.GetLogger();
            ThreadPool = parameters.GetThreadPool(_logger);

            Modules = new ModuleManager(this, _logger);
            EnvelopeFactory = new EnvelopeFactory();

            var dispatcherFactory = parameters.GetDispatchStrategyFactory();
            _pubSubStrategy = dispatcherFactory.CreatePubSubStrategy();
            _eavesdropStrategy = dispatcherFactory.CreatePubSubStrategy();
            _requestResponseStrategy = dispatcherFactory.CreateRequestResponseStrategy();
            _scatterGatherStrategy = dispatcherFactory.CreateScatterGatherStrategy();
        }

        public IModuleManager Modules { get; }
        public IThreadPool ThreadPool { get; }

        public IEnvelopeFactory EnvelopeFactory { get; }

        public void PublishEnvelope<TPayload>(Envelope<TPayload> message)
        {
            foreach (var channel in _pubSubStrategy.GetExistingChannels<TPayload>(message.Topic))
            {
                _logger.Debug("Publishing message Type={0} Topic={1} to channel Id={2}", typeof(TPayload).FullName, message.Topic, channel.Id);
                channel.Publish(message);
            }
            // TODO: Interceptors here so we can send messages to plugins or to federated instances
        }

        public IDisposable Subscribe<TPayload>(string topic, ISubscription<TPayload> subscription)
        {
            Assert.ArgumentNotNull(subscription, nameof(subscription));

            subscription.Id = Guid.NewGuid();
            var channel = _pubSubStrategy.GetChannelForSubscription<TPayload>(topic, _logger);
            _logger.Debug("Adding subscription {0} of type Type={1} Topic={2} to channel Id={3}", subscription.Id, typeof(TPayload).FullName, topic, channel.Id);
            return channel.Subscribe(subscription);
        }

        public TResponse RequestEnvelope<TRequest, TResponse>(Envelope<TRequest> request)
        {
            return RequestInternal<TRequest, TResponse>(request, _requestResponseStrategy).Response;
        }

        private CompleteResponse<TResponse> RequestInternal<TRequest, TResponse>(Envelope<TRequest> request, IReqResChannelDispatchStrategy strategy)
        {
            var channel = strategy.GetExistingChannel<TRequest, TResponse>(request.Topic);
            if (channel == null)
                return new CompleteResponse<TResponse>(default(TResponse), null);

            _logger.Debug("Requesting RequestType={0} ResponseType={1} Topic={2} to channel Id={3}", typeof(TRequest).FullName, typeof(TResponse).FullName, request.Topic, channel.Id);
            var waiter = channel.Request(request);

            bool complete = waiter.WaitForResponse();
            var response = new CompleteResponse<TResponse>(waiter.Response, waiter.ErrorInformation, complete); 
            waiter.Dispose();

            var eavesdropChannels = _eavesdropStrategy.GetExistingChannels<Conversation<TRequest, TResponse>>(request.Topic).ToList();
            if (eavesdropChannels.Any())
            {
                var conversation = new Conversation<TRequest, TResponse>(request.Payload, new List<TResponse> { response.Response });
                _logger.Debug("Eavesdropping on RequestType={0} ResponseType={1} Topic={2}, with {3} responses", typeof(TRequest).FullName, typeof(TResponse).FullName, request.Topic, conversation.Responses.Count);
                foreach (var eavesdropChannel in eavesdropChannels)
                {
                    _logger.Debug("Eavesdropping on RequestType={0} ResponseType={1} Topic={2}, on channel Id={3}", typeof(TRequest).FullName, typeof(TResponse).FullName, request.Topic, channel.Id);
                    var envelope = EnvelopeFactory.Create(null, conversation);
                    eavesdropChannel.Publish(envelope);
                }
            }

            return response;
        }

        public ScatterRequest<TResponse> Scatter<TRequest, TResponse>(string topic, TRequest request)
        {
            var scatter = new ScatterRequest<TResponse>();
            foreach (var channel in _scatterGatherStrategy.GetExistingChannels<TRequest, TResponse>(topic))
            {
                _logger.Debug("Requesting RequestType={0} ResponseType={1} Topic={2} to channel Id={3}", typeof(TRequest).FullName, typeof(TResponse).FullName, topic, channel.Id);
                channel.Scatter(request, scatter);
            }

            return scatter;
        }

        public IDisposable Listen<TRequest, TResponse>(string topic, IListener<TRequest, TResponse> listener)
        {
            Assert.ArgumentNotNull(listener, nameof(listener));

            listener.Id = Guid.NewGuid();
            var channel = _requestResponseStrategy.GetChannelForSubscription<TRequest, TResponse>(topic, _logger);
            _logger.Debug("Listener {0} RequestType={1} ResponseType={2} Topic={3} to channel Id={4}", listener.Id, typeof(TRequest).FullName, typeof(TResponse).FullName, topic, channel.Id);
            return channel.Listen(listener);
        }

        public IDisposable Eavesdrop<TRequest, TResponse>(string topic, ISubscription<Conversation<TRequest, TResponse>> subscription)
        {
            var channel = _eavesdropStrategy.GetChannelForSubscription<Conversation<TRequest, TResponse>>(topic, _logger);
            _logger.Debug("Eavesdrop on RequestType={0} ResponseType={1} Topic={2}, on channel Id={3}", typeof(TRequest).FullName, typeof(TResponse).FullName, topic, channel.Id);
            return channel.Subscribe(subscription);
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
            _pubSubStrategy.Dispose();
            _requestResponseStrategy.Dispose();
            _eavesdropStrategy.Dispose();
            _scatterGatherStrategy.Dispose();
            ThreadPool.Dispose();
        }
    }
}
