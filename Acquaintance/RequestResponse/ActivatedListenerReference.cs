using System;
using System.Threading;
using Acquaintance.Utility;

namespace Acquaintance.RequestResponse
{
    public class ActivatedListenerReference<TRequest, TResponse, TService> : IListenerReference<TRequest, TResponse>
        where TService: class
    {
        private readonly Func<TRequest, TService> _createService;
        private readonly Func<TService, TRequest, TResponse> _handler;
        private readonly bool _cacheService;
        private TService _service;

        public ActivatedListenerReference(Func<TRequest, TService> createService, Func<TService, TRequest, TResponse> handler, bool cacheService)
        {
            Assert.ArgumentNotNull(createService, nameof(createService));
            Assert.ArgumentNotNull(handler, nameof(handler));

            _createService = createService;
            _handler = handler;
            _cacheService = cacheService;
        }

        public bool IsAlive => true;

        public TResponse Invoke(Envelope<TRequest> request)
        {
            // TODO: An option to cache the service object
            var service = _service ?? _createService(request.Payload);
            if (service == null)
                throw new NullReferenceException("Activated service is null");
            var response = _handler(service, request.Payload);
            if (_cacheService && _service == null)
                Interlocked.CompareExchange(ref _service, service, null);
            return response;
        }
    }
}
