using System;

namespace Acquaintance.RequestResponse
{
    public class ImmediateListener<TRequest, TResponse> : IListener<TRequest, TResponse>
    {
        private readonly IListenerReference<TRequest, TResponse> _func;

        public ImmediateListener(IListenerReference<TRequest, TResponse> func)
        {
            _func = func;
        }

        public bool ShouldStopListening => !_func.IsAlive;

        public Guid Id { get; set; }

        public bool CanHandle(Envelope<TRequest> request)
        {
            return _func.IsAlive;
        }

        public void Request(Envelope<TRequest> envelope, IResponseReceiver<TResponse> request)
        {
            var value = _func.Invoke(envelope);
            request.SetResponse(value);
        }

        public static IListener<TRequest, TResponse> Create(Func<TRequest, TResponse> func)
        {
            return new ImmediateListener<TRequest, TResponse>(new PayloadStrongListenerReference<TRequest, TResponse>(func));
        }
    }
}