using Acquaintance.PubSub;
using Acquaintance.RequestResponse;
using Acquaintance.Threading;
using System;
using System.Collections.Generic;

namespace Acquaintance
{
    public sealed class SubscriptionCollection : IPubSubBus, IListenable, IDisposable
    {
        private readonly IMessageBus _messageBus;
        private readonly List<IDisposable> _subscriptions;

        public SubscriptionCollection(IMessageBus messageBus)
        {
            _messageBus = messageBus;
            _subscriptions = new List<IDisposable>();
        }

        public IThreadPool ThreadPool => _messageBus.ThreadPool;

        public ListenerFactory ListenerFactory => _messageBus.ListenerFactory;

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
        }

        public IDisposable Subscribe<TPayload>(string name, ISubscription<TPayload> subscription)
        {
            var token = _messageBus.Subscribe(name, subscription);
            _subscriptions.Add(token);
            return token;
        }

        public IDisposable Listen<TRequest, TResponse>(string name, IListener<TRequest, TResponse> listener)
        {
            var token = _messageBus.Listen(name, listener);
            _subscriptions.Add(token);
            return token;
        }

        public IDisposable Participate<TRequest, TResponse>(string name, IListener<TRequest, TResponse> listener)
        {
            var token = _messageBus.Participate(name, listener);
            _subscriptions.Add(token);
            return token;
        }

        public IDisposable Eavesdrop<TRequest, TResponse>(string name, ISubscription<Conversation<TRequest, TResponse>> subscriber)
        {
            var token = _messageBus.Eavesdrop(name, subscriber);
            _subscriptions.Add(token);
            return token;
        }

        public void Publish<TPayload>(string name, TPayload payload)
        {
            _messageBus.Publish<TPayload>(name, payload);
        }
    }
}
