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

        public bool CanHandle(TRequest request)
        {
            return _func.IsAlive;
        }

        public IDispatchableRequest<TResponse> Request(TRequest request)
        {
            var value = _func.Invoke(request);
            return new ImmediateResponse<TResponse>(value);
        }

        public static IListener<TRequest, TResponse> Create(Func<TRequest, TResponse> func)
        {
            return new ImmediateListener<TRequest, TResponse>(new StrongListenerReference<TRequest, TResponse>(func));
        }

        public bool ShouldStopListening => !_func.IsAlive;
    }
}