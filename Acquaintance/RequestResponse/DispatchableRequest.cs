using Acquaintance.Threading;
using System;
using System.Threading;

namespace Acquaintance.RequestResponse
{
    public class DispatchableRequest<TRequest, TResponse> : IThreadAction, IDispatchableRequest<TResponse>
    {
        private readonly IListenerReference<TRequest, TResponse> _func;
        private readonly TRequest _request;
        private readonly int _timeoutMs;
        private readonly ManualResetEvent _resetEvent;
        public TResponse Response { get; private set; }

        public DispatchableRequest(IListenerReference<TRequest, TResponse> func, TRequest request, int timeoutMs = 1000)
        {
            if (timeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeoutMs));
            _func = func;
            _request = request;
            _timeoutMs = timeoutMs;
            _resetEvent = new ManualResetEvent(false);
        }

        public void Execute()
        {
            Response = _func.Invoke(_request);
            _resetEvent.Set();
        }

        public bool WaitForResponse()
        {
            return _resetEvent.WaitOne(_timeoutMs);
        }

        public void Dispose()
        {
            _resetEvent.Dispose();
        }
    }
}