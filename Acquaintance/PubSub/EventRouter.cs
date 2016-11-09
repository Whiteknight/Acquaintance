using System;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.PubSub
{
    public class EventRouter<TPayload> : ISubscription<TPayload>
    {
        public IDisposable Token { get; set; }

        private class EventRoute
        {
            public EventRoute(string channelName, Func<TPayload, bool> predicate)
            {
                Predicate = predicate;
                ChannelName = channelName;
            }

            public Func<TPayload, bool> Predicate { get; }
            public string ChannelName { get; }
        }

        public EventRouter(IPubSubBus messageBus, string sourceName)
        {
            _sourceName = sourceName ?? string.Empty;
            _routes = new List<EventRoute>();
            _messageBus = messageBus;
        }

        private readonly string _sourceName;
        private readonly List<EventRoute> _routes;
        private readonly IPubSubBus _messageBus;

        public bool ShouldUnsubscribe => false;

        internal void SetToken(IDisposable token)
        {
            Token = new EventRouterSubscription(this, token);
        }

        public EventRouter<TPayload> Route(string name, Func<TPayload, bool> predicate, SubscribeOptions options = null)
        {
            name = name ?? string.Empty;
            if (name == _sourceName)
                throw new Exception("Circular reference detected. Cannot route on the same channel name.");

            _routes.Add(new EventRoute(name, predicate));
            return this;
        }

        // Wrapper to hold the IDisposable token but also hold a reference to the router so it
        // doesn't get GC collected
        private class EventRouterSubscription : IDisposable
        {
            private readonly EventRouter<TPayload> _router;
            private readonly IDisposable _token;

            public EventRouterSubscription(EventRouter<TPayload> router, IDisposable token)
            {
                _router = router;
                _token = token;
            }

            public void Dispose()
            {
                _token.Dispose();
            }
        }

        public void Publish(TPayload payload)
        {
            foreach (EventRoute route in _routes.Where(er => er.Predicate(payload)))
                _messageBus.Publish(route.ChannelName, payload);
        }
    }
}
