﻿using System;
using System.Threading;
using Acquaintance.Utility;

namespace Acquaintance.PubSub
{
    public class MaxEventsSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly ISubscription<TPayload> _inner;
        private int _maxEvents;

        public MaxEventsSubscription(ISubscription<TPayload> inner, int maxEvents)
        {
            Assert.ArgumentNotNull(inner, nameof(inner));

            _inner = inner;
            _maxEvents = maxEvents;
        }

        public Guid Id
        {
            get => _inner.Id;
            set => _inner.Id = value;
        }

        public void Publish(Envelope<TPayload> message)
        {
            int maxEvents = Interlocked.Decrement(ref _maxEvents);
            if (maxEvents >= 0)
                _inner.Publish(message);
        }

        public bool ShouldUnsubscribe => _inner.ShouldUnsubscribe || _maxEvents <= 0;
    }
}