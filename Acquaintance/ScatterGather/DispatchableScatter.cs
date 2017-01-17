using Acquaintance.Threading;
using System;
using System.Threading;

namespace Acquaintance.ScatterGather
{
    public class DispatchableScatter<TRequest, TResponse> : IThreadAction, IDispatchableScatter<TResponse>
    {
        private readonly IParticipantReference<TRequest, TResponse> _func;
        private readonly TRequest _request;
        private readonly int _timeoutMs;
        private readonly ManualResetEvent _resetEvent;

        public DispatchableScatter(IParticipantReference<TRequest, TResponse> func, TRequest request, Guid participantId, int timeoutMs = 1000)
        {
            if (timeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeoutMs));
            _func = func;
            _request = request;
            _timeoutMs = timeoutMs;
            _resetEvent = new ManualResetEvent(false);
            Responses = new TResponse[0];
            ParticipantId = participantId;
        }

        public TResponse[] Responses { get; private set; }
        public bool Success { get; private set; }
        public Exception ErrorInformation { get; private set; }
        public Guid ParticipantId { get; }

        public void Execute()
        {
            try
            {
                Responses = _func.Invoke(_request);
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