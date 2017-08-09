using Acquaintance.PubSub;
using Acquaintance.RequestResponse;
using Acquaintance.ScatterGather;
using Acquaintance.Threading;
using System;
using System.Collections.Generic;

namespace Acquaintance
{
    /// <summary>
    /// SubscriptionCollection holds a collection of subscription tokens so that they can all be
    /// disposed at once.
    /// </summary>
    public sealed class SubscriptionCollection : IPubSubBus, IReqResBus, IScatterGatherBus, IDisposable
    {
        private readonly IMessageBus _messageBus;
        private readonly List<IDisposable> _subscriptions;

        public SubscriptionCollection(IMessageBus messageBus)
        {
            _messageBus = messageBus;
            _subscriptions = new List<IDisposable>();
        }

        public IThreadPool ThreadPool => _messageBus.ThreadPool;

        public IEnvelopeFactory EnvelopeFactory => _messageBus.EnvelopeFactory;

        public IDisposable Subscribe<TPayload>(string channelName, ISubscription<TPayload> subscription)
        {
            var token = _messageBus.Subscribe(channelName, subscription);
            _subscriptions.Add(token);
            return token;
        }

        public IDisposable Listen<TRequest, TResponse>(string channelName, IListener<TRequest, TResponse> listener)
        {
            var token = _messageBus.Listen(channelName, listener);
            _subscriptions.Add(token);
            return token;
        }

        public IDisposable Participate<TRequest, TResponse>(string channelName, IParticipant<TRequest, TResponse> participant)
        {
            var token = _messageBus.Participate(channelName, participant);
            _subscriptions.Add(token);
            return token;
        }

        public IDisposable Eavesdrop<TRequest, TResponse>(string channelName, ISubscription<Conversation<TRequest, TResponse>> subscriber)
        {
            var token = _messageBus.Eavesdrop(channelName, subscriber);
            _subscriptions.Add(token);
            return token;
        }

        public TResponse RequestEnvelope<TRequest, TResponse>(Envelope<TRequest> request)
        {
            return _messageBus.RequestEnvelope<TRequest, TResponse>(request);
        }

        public IGatheredResponse<TResponse> Scatter<TRequest, TResponse>(string channelName, TRequest request)
        {
            return _messageBus.Scatter<TRequest, TResponse>(channelName, request);
        }

        public void PublishEnvelope<TPayload>(Envelope<TPayload> envelope)
        {
            _messageBus.PublishEnvelope(envelope);
        }

        public void Clear()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
            _subscriptions.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
