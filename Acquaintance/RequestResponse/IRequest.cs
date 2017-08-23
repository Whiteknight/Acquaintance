using System;

namespace Acquaintance.RequestResponse
{
    public interface IRequest<out TResponse>
    {
        bool WaitForResponse(TimeSpan timeout);
        TResponse GetResponse();
        object GetResponseObject();
        Exception GetErrorInformation();
        bool IsComplete();
        bool HasResponse();
        void ThrowExceptionIfError();
        bool WaitForResponse();
    }
}