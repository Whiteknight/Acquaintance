﻿using System;
using System.Threading;

namespace Acquaintance.PubSub
{
    public class MaxEventsSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly ISubscription<TPayload> _inner;
        private int _maxEvents;

        public MaxEventsSubscription(ISubscription<TPayload> inner, int maxEvents)
        {
            if (inner == null)
                throw new System.ArgumentNullException(nameof(inner));

            _inner = inner;
            _maxEvents = maxEvents;
        }

        public Guid Id
        {
            get { return _inner.Id; }
            set { _inner.Id = value; }
        }

        public void Publish(TPayload payload)
        {
            int maxEvents = Interlocked.Decrement(ref _maxEvents);
            if (maxEvents >= 0)
                _inner.Publish(payload);
        }

        public bool ShouldUnsubscribe => _inner.ShouldUnsubscribe || _maxEvents <= 0;
    }
}