using Acquaintance.Threading;
using System;
using System.Threading;

namespace Acquaintance.ScatterGather
{
    public class DispatchableScatter<TRequest, TResponse> : IThreadAction, IDispatchableRequest<TResponse>
    {
        private readonly IParticipantReference<TRequest, TResponse> _func;
        private readonly TRequest _request;
        private readonly int _timeoutMs;
        private readonly ManualResetEvent _resetEvent;
        public TResponse[] Responses { get; private set; }

        public DispatchableScatter(IParticipantReference<TRequest, TResponse> func, TRequest request, int timeoutMs = 1000)
        {
            if (timeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeoutMs));
            _func = func;
            _request = request;
            _timeoutMs = timeoutMs;
            _resetEvent = new ManualResetEvent(false);
            Responses = new TResponse[0];
        }

        public void Execute()
        {
            Responses = _func.Invoke(_request);
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