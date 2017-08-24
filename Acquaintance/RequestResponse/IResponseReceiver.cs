using System;

namespace Acquaintance.RequestResponse
{
    public interface IResponseReceiver<in TResponse>
    {
        void SetNoResponse();
        void SetResponse(TResponse response);
        void SetError(Exception e);
    }
}