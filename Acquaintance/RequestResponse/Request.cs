using System;
using System.Threading;
using System.Threading.Tasks;

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

    public interface IRequest<out TResponse>
    {
        bool WaitForResponse(TimeSpan timeout);
        TResponse GetResponse();
        object GetResponseObject();
        Exception GetErrorInformation();
        bool HasResponse();
        void ThrowExceptionIfError();
        bool WaitForResponse();
    }

    public static class RequestExtensions
    {
        public static TResponse GetResponseOrWait<TResponse>(this IRequest<TResponse> request, TimeSpan timeout)
        {
            request.WaitForResponse(timeout);
            request.ThrowExceptionIfError();
            return request.GetResponse();
        }

        public static TResponse GetResponseOrWait<TResponse>(this IRequest<TResponse> request)
        {
            return GetResponseOrWait(request, new TimeSpan(0, 0, 10));
        }

        public static async Task<TResponse> ToTask<TResponse>(this IRequest<TResponse> request)
        {
            return await Task
                .Run(() =>
                {
                    request.WaitForResponse();
                    request.ThrowExceptionIfError();
                    return request.GetResponse();
                })
                .ConfigureAwait(false);
        }
    }
    
    public class Request<TResponse> : Request, IRequest<TResponse>
    {
        private readonly ManualResetEvent _resetEvent;

        private TResponse _response;
        private Exception _exception;
        private int _timesSet;
        private bool _hasResponse;

        public Request()
        {
            _timesSet = 0;
            _resetEvent = new ManualResetEvent(false);
        }

        public static Request<TResponse> WithNoResponse()
        {
            var request = new Request<TResponse>();
            request.SetNoResponse();
            return request;
        }

        public void SetNoResponse()
        {
            var canSet = Interlocked.Increment(ref _timesSet);
            if (canSet == 1)
            {
                _hasResponse = true;
                _resetEvent.Set();
            }
        }

        public void SetResponse(TResponse response)
        {
            var canSet = Interlocked.Increment(ref _timesSet);
            if (canSet == 1)
            {
                _response = response;
                _hasResponse = true;
                _exception = null;
                _resetEvent.Set();
            }
        }

        public void SetError(Exception e)
        {
            var canSet = Interlocked.Increment(ref _timesSet);
            if (canSet == 1)
            {
                _response = default(TResponse);
                _exception = e;
                _hasResponse = true;
                _resetEvent.Set();
            }
        }

        public override bool WaitForResponse(TimeSpan timeout)
        {
            if (_hasResponse)
                return true;
            bool ok = _resetEvent.WaitOne(timeout);
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
