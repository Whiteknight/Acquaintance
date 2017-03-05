﻿using System;

namespace Acquaintance.PubSub
{
    public class ActivatedSubscriberReference<TPayload, TService> : ISubscriberReference<TPayload>
    {
        private readonly Func<TPayload, TService> _createService;
        private readonly Action<TService, TPayload> _handler;

        public ActivatedSubscriberReference(Func<TPayload, TService> createService, Action<TService, TPayload> handler)
        {
            if (createService == null)
                throw new ArgumentNullException(nameof(createService));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _createService = createService;
            _handler = handler;
        }

        public bool IsAlive => true;

        public void Invoke(Envelope<TPayload> message)
        {
            var service = _createService(message.Payload);
            if (service == null)
                throw new NullReferenceException("Activated service is null");
            _handler(service, message.Payload);
        }
    }
}
