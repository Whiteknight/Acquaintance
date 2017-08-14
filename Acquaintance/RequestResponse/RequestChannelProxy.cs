using System;
using System.Threading.Tasks;
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

        public IRequest<TResponse> Request(TRequest request)
        {
            return _messageBus.Request<TRequest, TResponse>(request);
        }

        public IRequest<TResponse> Request(string topic, TRequest request)
        {
            return _messageBus.Request<TRequest, TResponse>(topic, request);
        }

        public TResponse RequestWait(TRequest request)
        {
            return _messageBus.Request<TRequest, TResponse>(request).GetResponseOrWait();
        }

        public TResponse RequestWait(string topic, TRequest request)
        {
            return _messageBus.Request<TRequest, TResponse>(topic, request).GetResponseOrWait();
        }

        public Task<TResponse> RequestAsync(string topic, TRequest request)
        {
            return _messageBus.RequestAsync<TRequest, TResponse>(topic, request);
        }

        public Task<TResponse> RequestAsync(TRequest request)
        {
            return _messageBus.RequestAsync<TRequest, TResponse>(string.Empty, request);
        }

        public TResponse Request(Envelope<TRequest> envelope)
        {
            var request = _messageBus.RequestEnvelope<TRequest, TResponse>(envelope);
            request.WaitForResponse();
            return request.GetResponse();
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
