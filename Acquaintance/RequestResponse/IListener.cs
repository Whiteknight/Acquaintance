using System;

namespace Acquaintance.RequestResponse
{
    public interface IListener<in TRequest, out TResponse>
    {
        bool CanHandle(TRequest request);
        IDispatchableRequest<TResponse> Request(TRequest request);
        bool ShouldStopListening { get; }
        Guid Id { get; set; }
    }
}