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
            foreach (var channel in _pubSubStrategy.GetExistingChannels<TPayload>(message.Channel))
            {
                _logger.Debug("Publishing message Type={0} ChannelName={1} to channel Id={2}", typeof(TPayload).FullName, message.Channel, channel.Id);
                channel.Publish(message);
            }
            // TODO: Interceptors here so we can send messages to plugins or to federated instances
        }

        public IDisposable Subscribe<TPayload>(string channelName, ISubscription<TPayload> subscription)
        {
            Assert.ArgumentNotNull(subscription, nameof(subscription));

            subscription.Id = Guid.NewGuid();
            var channel = _pubSubStrategy.GetChannelForSubscription<TPayload>(channelName, _logger);
            _logger.Debug("Adding subscription {0} of type Type={1} ChannelName={2} to channel Id={3}", subscription.Id, typeof(TPayload).FullName, channelName, channel.Id);
            return channel.Subscribe(subscription);
        }

        public TResponse RequestEnvelope<TRequest, TResponse>(Envelope<TRequest> request)
        {
            return RequestInternal<TRequest, TResponse>(request, _requestResponseStrategy).Response;
        }

        private CompleteResponse<TResponse> RequestInternal<TRequest, TResponse>(Envelope<TRequest> request, IReqResChannelDispatchStrategy strategy)
        {
            var channel = strategy.GetExistingChannel<TRequest, TResponse>(request.Channel);
            if (channel == null)
                return new CompleteResponse<TResponse>(default(TResponse), null);

            _logger.Debug("Requesting RequestType={0} ResponseType={1} ChannelName={2} to channel Id={3}", typeof(TRequest).FullName, typeof(TResponse).FullName, request.Channel, channel.Id);
            var waiter = channel.Request(request);

            CompleteResponse<TResponse> response;
            bool complete = waiter.WaitForResponse();
            if (complete)
                response = new CompleteResponse<TResponse>(waiter.Response, waiter.ErrorInformation);
            else
                response = new CompleteResponse<TResponse>(default(TResponse), null, false);
            waiter.Dispose();

            var eavesdropChannels = _eavesdropStrategy.GetExistingChannels<Conversation<TRequest, TResponse>>(request.Channel).ToList();
            if (eavesdropChannels.Any())
            {
                var conversation = new Conversation<TRequest, TResponse>(request.Payload, new List<TResponse> { response.Response });
                _logger.Debug("Eavesdropping on RequestType={0} ResponseType={1} ChannelName={2}, with {3} responses", typeof(TRequest).FullName, typeof(TResponse).FullName, request.Channel, conversation.Responses.Count);
                foreach (var eavesdropChannel in eavesdropChannels)
                {
                    _logger.Debug("Eavesdropping on RequestType={0} ResponseType={1} ChannelName={2}, on channel Id={3}", typeof(TRequest).FullName, typeof(TResponse).FullName, request.Channel, channel.Id);
                    var envelope = EnvelopeFactory.Create(null, conversation);
                    eavesdropChannel.Publish(envelope);
                }
            }

            return response;
        }

        public IGatheredResponse<TResponse> Scatter<TRequest, TResponse>(string channelName, TRequest request)
        {
            return ScatterInternal<TRequest, TResponse>(channelName, request, _scatterGatherStrategy);
        }

        private IGatheredResponse<TResponse> ScatterInternal<TRequest, TResponse>(string name, TRequest request, IScatterGatherChannelDispatchStrategy strategy)
        {
            var waiters = new List<IDispatchableScatter<TResponse>>();
            // TODO: Keep track of how much time is spent on each channel, and subtract that from the time available to
            // the next channel
            foreach (var channel in strategy.GetExistingChannels<TRequest, TResponse>(name))
            {
                _logger.Debug("Requesting RequestType={0} ResponseType={1} ChannelName={2} to channel Id={3}", typeof(TRequest).FullName, typeof(TResponse).FullName, name, channel.Id);
                waiters.AddRange(channel.Scatter(request));
            }

            var responses = new List<CompleteGather<TResponse>>();
            var rawResponses = new List<TResponse>();
            foreach (var waiter in waiters)
            {
                bool complete = waiter.WaitForResponse();
                if (complete)
                {
                    responses.Add(new CompleteGather<TResponse>(waiter.Responses, waiter.ErrorInformation));
                    rawResponses.AddRange(waiter.Responses);
                }
                else
                    responses.Add(new CompleteGather<TResponse>(null, null, false));

                waiter.Dispose();
            }

            var response = new GatheredResponse<TResponse>(responses);

            var eavesdropChannels = _eavesdropStrategy.GetExistingChannels<Conversation<TRequest, TResponse>>(name).ToList();
            if (eavesdropChannels.Any())
            {
                var conversation = new Conversation<TRequest, TResponse>(request, rawResponses);
                _logger.Debug("Eavesdropping on RequestType={0} ResponseType={1} ChannelName={2}, with {3} responses", typeof(TRequest).FullName, typeof(TResponse).FullName, name, conversation.Responses.Count);
                foreach (var channel in eavesdropChannels)
                {
                    _logger.Debug("Eavesdropping on RequestType={0} ResponseType={1} ChannelName={2}, on channel Id={3}", typeof(TRequest).FullName, typeof(TResponse).FullName, name, channel.Id);
                    var envelope = EnvelopeFactory.Create(null, conversation);
                    channel.Publish(envelope);
                }
            }

            return response;
        }

        public IDisposable Listen<TRequest, TResponse>(string channelName, IListener<TRequest, TResponse> listener)
        {
            Assert.ArgumentNotNull(listener, nameof(listener));

            listener.Id = Guid.NewGuid();
            var channel = _requestResponseStrategy.GetChannelForSubscription<TRequest, TResponse>(channelName, _logger);
            _logger.Debug("Listener {0} RequestType={1} ResponseType={2} ChannelName={3} to channel Id={4}", listener.Id, typeof(TRequest).FullName, typeof(TResponse).FullName, channelName, channel.Id);
            return channel.Listen(listener);
        }

        public IDisposable Eavesdrop<TRequest, TResponse>(string channelName, ISubscription<Conversation<TRequest, TResponse>> subscription)
        {
            var channel = _eavesdropStrategy.GetChannelForSubscription<Conversation<TRequest, TResponse>>(channelName, _logger);
            _logger.Debug("Eavesdrop on RequestType={0} ResponseType={1} ChannelName={2}, on channel Id={3}", typeof(TRequest).FullName, typeof(TResponse).FullName, channelName, channel.Id);
            return channel.Subscribe(subscription);
        }

        public IDisposable Participate<TRequest, TResponse>(string channelName, IParticipant<TRequest, TResponse> participant)
        {
            Assert.ArgumentNotNull(participant, nameof(participant));

            participant.Id = Guid.NewGuid();
            var channel = _scatterGatherStrategy.GetChannelForSubscription<TRequest, TResponse>(channelName, _logger);
            _logger.Debug("Participant {0} on RequestType={1} ResponseType={2} ChannelName={3}, on channel Id={4}", participant.Id, typeof(TRequest).FullName, typeof(TResponse).FullName, channelName, channel.Id);
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
