using Acquaintance.PubSub;
using Acquaintance.RequestResponse;
using System;
using System.Collections.Generic;

namespace Acquaintance
{
    public sealed class SubscriptionCollection : ISubscribable, IListenable, IDisposable
    {
        private readonly IMessageBus _messageBus;
        private readonly List<IDisposable> _subscriptions;

        public SubscriptionCollection(IMessageBus messageBus)
        {
            _messageBus = messageBus;
            _subscriptions = new List<IDisposable>();
        }

        public SubscriptionFactory SubscriptionFactory => _messageBus.SubscriptionFactory;

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

        public IDisposable Eavesdrop<TRequest, TResponse>(string name, Action<Conversation<TRequest, TResponse>> subscriber, Func<Conversation<TRequest, TResponse>, bool> filter, SubscribeOptions options = null)
        {
            var token = _messageBus.Eavesdrop(name, subscriber, filter, options);
            _subscriptions.Add(token);
            return token;
        }


    }
}
