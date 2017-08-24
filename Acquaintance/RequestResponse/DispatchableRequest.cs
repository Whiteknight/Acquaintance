using Acquaintance.Threading;
using System;

namespace Acquaintance.RequestResponse
{
    public class DispatchableRequest<TRequest, TResponse> : IThreadAction, IDispatchableRequest
    {
        private readonly IListenerReference<TRequest, TResponse> _func;
        private readonly Envelope<TRequest> _envelope;
        private readonly IResponseReceiver<TResponse> _request;

        public DispatchableRequest(IListenerReference<TRequest, TResponse> func, Envelope<TRequest> envelope, Guid listenerId, IResponseReceiver<TResponse> request)
        {
            _func = func;
            _envelope = envelope;
            _request = request;
            ListenerId = listenerId;
        }

        public Guid ListenerId { get; }

        public void Execute()
        {
            try
            {
                var response = _func.Invoke(_envelope);
                _request.SetResponse(response);
            }
            catch (Exception e)
            {
                _request.SetError(e);
            }
        }
    }
}