using System;
using System.Threading;
using Acquaintance.Utility;

namespace Acquaintance.PubSub
{
    public class ActivatedSubscriberReference<TPayload, TService> : ISubscriberReference<TPayload>
        where TService : class
    {
        private readonly Func<TPayload, TService> _createService;
        private readonly Action<TService, TPayload> _handler;
        private readonly bool _cacheInstance;
        private TService _service;

        public ActivatedSubscriberReference(Func<TPayload, TService> createService, Action<TService, TPayload> handler, bool cacheInstance)
        {
            Assert.ArgumentNotNull(createService, nameof(createService));
            Assert.ArgumentNotNull(handler, nameof(handler));

            _createService = createService;
            _handler = handler;
            _cacheInstance = cacheInstance;
        }

        public bool IsAlive => true;

        public void Invoke(Envelope<TPayload> message)
        {
            // TODO: We should have the option to cache the service instance
            var service = _service ?? _createService(message.Payload);
            if (service == null)
                throw new NullReferenceException("Activated service is null");
            _handler(service, message.Payload);
            if (_cacheInstance)
                Interlocked.CompareExchange(ref _service, service, null);
        }
    }
}
