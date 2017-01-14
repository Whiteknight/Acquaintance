using Acquaintance.Logging;
using Acquaintance.Modules;
using Acquaintance.PubSub;
using Acquaintance.RequestResponse;
using Acquaintance.ScatterGather;
using Acquaintance.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Acquaintance
{
    /// <summary>
    /// The message bus object, which coordinates communication features.
    /// </summary>
    public sealed class MessageBus : IMessageBus
    {
        private readonly ILogger _logger;
        public IThreadPool ThreadPool { get; }
        private readonly IPubSubChannelDispatchStrategy _pubSubStrategy;
        private readonly IPubSubChannelDispatchStrategy _eavesdropStrategy;
        private readonly IReqResChannelDispatchStrategy _requestResponseStrategy;
        private readonly IScatterGatherChannelDispatchStrategy _scatterGatherStrategy;

        public MessageBus(IThreadPool threadPool = null, ILogger logger = null, IDispatchStrategyFactory dispatcherFactory = null)
        {
            _logger = logger ?? CreateDefaultLogger();
            ThreadPool = threadPool ?? new MessagingWorkerThreadPool(2);
            Modules = new ModuleManager(this, _logger);

            dispatcherFactory = dispatcherFactory ?? new SimpleDispatchStrategyFactory();
            _pubSubStrategy = dispatcherFactory.CreatePubSubStrategy();
            _eavesdropStrategy = dispatcherFactory.CreatePubSubStrategy();
            _requestResponseStrategy = dispatcherFactory.CreateRequestResponseStrategy();
            _scatterGatherStrategy = dispatcherFactory.CreateScatterGatherStrategy();
        }

        public IModuleManager Modules { get; }

        public int StartDedicatedWorkerThread()
        {
            int id = ThreadPool.StartDedicatedWorker();
            _logger.Debug("Starting dedicated worker thread {0}", id);
            return id;
        }

        public void StopDedicatedWorkerThread(int id)
        {
            _logger.Debug("Stopping dedicated worker thread {0}", id);
            ThreadPool.StopDedicatedWorker(id);
        }

        public void Publish<TPayload>(string channelName, TPayload payload)
        {
            foreach (var channel in _pubSubStrategy.GetExistingChannels<TPayload>(channelName))
            {
                _logger.Debug("Publishing message Type={0} ChannelName={1} to channel Id={2}", typeof(TPayload).FullName, channelName, channel.Id);
                channel.Publish(payload);
            }
        }

        public IDisposable Subscribe<TPayload>(string channelName, ISubscription<TPayload> subscription)
        {
            var channel = _pubSubStrategy.GetChannelForSubscription<TPayload>(channelName);
            _logger.Debug("Adding subscription of type Type={0} ChannelName={1} to channel Id={2}", typeof(TPayload).FullName, channelName, channel.Id);
            return channel.Subscribe(subscription);
        }

        public TResponse Request<TRequest, TResponse>(string channelName, TRequest request)
        {
            return RequestInternal<TRequest, TResponse>(channelName, request, _requestResponseStrategy);
        }

        private TResponse RequestInternal<TRequest, TResponse>(string name, TRequest request, IReqResChannelDispatchStrategy strategy)
        {
            var response = default(TResponse);
            var channel = strategy.GetExistingChannel<TRequest, TResponse>(name);
            if (channel == null)
                return response;

            _logger.Debug("Requesting RequestType={0} ResponseType={1} ChannelName={2} to channel Id={3}", typeof(TRequest).FullName, typeof(TResponse).FullName, name, channel.Id);
            var waiter = channel.Request(request);

            bool complete = waiter.WaitForResponse();
            if (complete)
                response = waiter.Response;
            waiter.Dispose();

            var eavesdropChannels = _eavesdropStrategy.GetExistingChannels<Conversation<TRequest, TResponse>>(name).ToList();
            if (eavesdropChannels.Any())
            {
                var conversation = new Conversation<TRequest, TResponse>(request, new List<TResponse> { response });
                _logger.Debug("Eavesdropping on RequestType={0} ResponseType={1} ChannelName={2}, with {3} responses", typeof(TRequest).FullName, typeof(TResponse).FullName, name, conversation.Responses.Count);
                foreach (var eavesdropChannel in eavesdropChannels)
                {
                    _logger.Debug("Eavesdropping on RequestType={0} ResponseType={1} ChannelName={2}, on channel Id={3}", typeof(TRequest).FullName, typeof(TResponse).FullName, name, channel.Id);
                    eavesdropChannel.Publish(conversation);
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

            var responses = new List<TResponse>();
            foreach (var waiter in waiters)
            {
                bool complete = waiter.WaitForResponse();
                if (complete)
                    responses.AddRange(waiter.Responses);
                waiter.Dispose();
            }

            var eavesdropChannels = _eavesdropStrategy.GetExistingChannels<Conversation<TRequest, TResponse>>(name).ToList();
            if (eavesdropChannels.Any())
            {
                var conversation = new Conversation<TRequest, TResponse>(request, responses);
                _logger.Debug("Eavesdropping on RequestType={0} ResponseType={1} ChannelName={2}, with {3} responses", typeof(TRequest).FullName, typeof(TResponse).FullName, name, conversation.Responses.Count);
                foreach (var channel in eavesdropChannels)
                {
                    _logger.Debug("Eavesdropping on RequestType={0} ResponseType={1} ChannelName={2}, on channel Id={3}", typeof(TRequest).FullName, typeof(TResponse).FullName, name, channel.Id);
                    channel.Publish(conversation);
                }
            }

            return new GatheredResponse<TResponse>(responses);
        }

        public IDisposable Listen<TRequest, TResponse>(string channelName, IListener<TRequest, TResponse> listener)
        {
            var channel = _requestResponseStrategy.GetChannelForSubscription<TRequest, TResponse>(channelName);
            _logger.Debug("Listening RequestType={0} ResponseType={1} ChannelName={2} to channel Id={3}", typeof(TRequest).FullName, typeof(TResponse).FullName, channelName, channel.Id);
            return channel.Listen(listener);
        }

        public IDisposable Eavesdrop<TRequest, TResponse>(string channelName, ISubscription<Conversation<TRequest, TResponse>> subscription)
        {
            var channel = _eavesdropStrategy.GetChannelForSubscription<Conversation<TRequest, TResponse>>(channelName);
            _logger.Debug("Eavesdrop on RequestType={0} ResponseType={1} ChannelName={2}, on channel Id={3}", typeof(TRequest).FullName, typeof(TResponse).FullName, channelName, channel.Id);
            return channel.Subscribe(subscription);
        }

        public IDisposable Participate<TRequest, TResponse>(string channelName, IParticipant<TRequest, TResponse> participant)
        {
            var channel = _scatterGatherStrategy.GetChannelForSubscription<TRequest, TResponse>(channelName);
            _logger.Debug("Participating on RequestType={0} ResponseType={1} ChannelName={2}, on channel Id={3}", typeof(TRequest).FullName, typeof(TResponse).FullName, channelName, channel.Id);
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

        private static ILogger CreateDefaultLogger()
        {
#if DEBUG
            return new DelegateLogger(s => Debug.WriteLine(s));
#else
            return new SilentLogger();
#endif
        }
    }
}
