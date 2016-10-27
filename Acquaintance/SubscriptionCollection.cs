using System;
using System.Collections.Generic;

namespace Acquaintance
{
    public sealed class SubscriptionCollection : ISubscribable, IDisposable
    {
        private readonly IMessageBus _messageBus;
        private readonly List<IDisposable> _subscriptions;

        public SubscriptionCollection(IMessageBus messageBus)
        {
            _messageBus = messageBus;
            _subscriptions = new List<IDisposable>();
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
        }

        public IDisposable Subscribe<TPayload>(string name, Action<TPayload> subscriber, Func<TPayload, bool> filter, SubscribeOptions options = null)
        {
            var token = _messageBus.Subscribe(name, subscriber, filter, options);
            _subscriptions.Add(token);
            return token;
        }

        public IDisposable Subscribe<TRequest, TResponse>(string name, Func<TRequest, TResponse> subscriber, Func<TRequest, bool> filter, SubscribeOptions options = null)
        {
            var token = _messageBus.Subscribe(name, subscriber, filter, options);
            _subscriptions.Add(token);
            return token;
        }
    }
}
