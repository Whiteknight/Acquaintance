using System;
using Acquaintance.Utility;

namespace Acquaintance.RequestResponse
{
    public class ActivatedListenerReference<TRequest, TResponse, TService> : IListenerReference<TRequest, TResponse>
    {
        private readonly Func<TRequest, TService> _createService;
        private readonly Func<TService, TRequest, TResponse> _handler;

        public ActivatedListenerReference(Func<TRequest, TService> createService, Func<TService, TRequest, TResponse> handler)
        {
            Assert.ArgumentNotNull(createService, nameof(createService));
            Assert.ArgumentNotNull(handler, nameof(handler));

            _createService = createService;
            _handler = handler;
        }

        public bool IsAlive => true;

        public TResponse Invoke(Envelope<TRequest> request)
        {
            var service = _createService(request.Payload);
            if (service == null)
                throw new NullReferenceException("Activated service is null");
            return _handler(service, request.Payload);
        }
    }
}
