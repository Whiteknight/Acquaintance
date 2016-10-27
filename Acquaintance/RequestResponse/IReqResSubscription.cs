namespace Acquaintance.RequestResponse
{
    public interface IReqResSubscription<in TRequest, out TResponse>
    {
        bool CanHandle(TRequest request);
        IDispatchableRequest<TResponse> Request(TRequest request);
    }
}