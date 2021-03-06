﻿using System;
using System.Threading;

namespace Acquaintance.RequestResponse
{
    public abstract class Request
    {
        public bool WaitForResponse()
        {
            return WaitForResponse(new TimeSpan(0, 0, 10));
        }

        public abstract bool WaitForResponse(TimeSpan timeout);

        public abstract object GetResponseObject();

        public abstract void ThrowExceptionIfError();
    }

    public class Request<TResponse> : Request, IRequest<TResponse>, IResponseReceiver<TResponse>
    {
        private readonly ManualResetEvent _resetEvent;

        private TResponse _response;
        private volatile Exception _exception;
        private int _timesSet;
        private volatile bool _isComplete;
        private volatile bool _hasResponse;
        private int _cleanupAttempts;

        public Request()
        {
            _timesSet = 0;
            _cleanupAttempts = 0;
            _resetEvent = new ManualResetEvent(false);
        }

        public void SetNoResponse()
        {
            var canSet = Interlocked.Increment(ref _timesSet);
            if (canSet == 1)
            {
                _isComplete = true;
                _hasResponse = false;
                Interlocked.MemoryBarrier();
                _resetEvent.Set();
            }
        }

        public void SetResponse(TResponse response)
        {
            var canSet = Interlocked.Increment(ref _timesSet);
            if (canSet != 1)
                return;
            _response = response;
            _exception = null;
            _hasResponse = true;
            Interlocked.MemoryBarrier();
            _isComplete = true;
            Interlocked.MemoryBarrier();
            _resetEvent.Set();
        }

        public void SetError(Exception e)
        {
            var canSet = Interlocked.Increment(ref _timesSet);
            if (canSet != 1)
                return;
            _response = default(TResponse);
            _exception = e;
            _hasResponse = true;
            Interlocked.MemoryBarrier();
            _isComplete = true;
            Interlocked.MemoryBarrier();
            _resetEvent.Set();
        }

        public override bool WaitForResponse(TimeSpan timeout)
        {
            if (_isComplete)
                return true;
            bool ok = _resetEvent.WaitOne(timeout);
            if (Interlocked.CompareExchange(ref _cleanupAttempts, 1, 0) == 0)
                _resetEvent.Dispose();
            return ok;
        }

        public TResponse GetResponse()
        {
            return _response;
        }

        public override object GetResponseObject()
        {
            return GetResponse();
        }

        public Exception GetErrorInformation()
        {
            return _exception;
        }

        public bool IsComplete()
        {
            return _isComplete;
        }

        public bool HasResponse()
        {
            return _hasResponse;
        }

        public override void ThrowExceptionIfError()
        {
            if (_exception != null)
                throw _exception;
        }
    }
}
