using System;
using Acquaintance.Utility;

namespace Acquaintance.RequestResponse
{
    public class RequestChannelProxy<TRequest, TResponse>
    {
        private readonly IReqResBus _messageBus;

        public RequestChannelProxy(IReqResBus messageBus)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));

            _messageBus = messageBus;
        }

        public TResponse Request(TRequest request)
        {
            return _messageBus.Request<TRequest, TResponse>(request);
        }

        public TResponse Request(string topic, TRequest request)
        {
            return _messageBus.Request<TRequest, TResponse>(topic, request);
        }

        public TResponse Request(Envelope<TRequest> request)
        {
            return _messageBus.RequestEnvelope<TRequest, TResponse>(request);
        }

        public IDisposable Listen(string topic, IListener<TRequest, TResponse> listener)
        {
            return _messageBus.Listen(topic, listener);
        }

        public IDisposable Listen(Action<ITopicListenerBuilder<TRequest, TResponse>> build)
        {
            return _messageBus.Listen(build);
        }
    }
}
