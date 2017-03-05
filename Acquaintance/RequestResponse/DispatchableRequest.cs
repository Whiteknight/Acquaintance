using Acquaintance.Threading;
using System;
using System.Threading;

namespace Acquaintance.RequestResponse
{
    public class DispatchableRequest<TRequest, TResponse> : IThreadAction, IDispatchableRequest<TResponse>
    {
        private readonly IListenerReference<TRequest, TResponse> _func;
        private readonly Envelope<TRequest> _request;
        private readonly int _timeoutMs;
        private readonly ManualResetEvent _resetEvent;

        public DispatchableRequest(IListenerReference<TRequest, TResponse> func, Envelope<TRequest> request, Guid listenerId, int timeoutMs = 1000)
        {
            if (timeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeoutMs));
            _func = func;
            _request = request;
            _timeoutMs = timeoutMs;
            _resetEvent = new ManualResetEvent(false);
            ListenerId = listenerId;
        }

        public TResponse Response { get; private set; }
        public bool Success { get; private set; }
        public Exception ErrorInformation { get; private set; }

        public Guid ListenerId { get; }

        public void Execute()
        {
            try
            {
                Response = _func.Invoke(_request);
                Success = true;
            }
            catch (Exception e)
            {
                Success = false;
                ErrorInformation = e;
            }
            finally
            {
                _resetEvent.Set();
            }
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