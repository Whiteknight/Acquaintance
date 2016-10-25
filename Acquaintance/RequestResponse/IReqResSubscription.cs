namespace Acquaintance.RequestResponse
{
    public interface IReqResSubscription<in TRequest, out TResponse>
        where TRequest : IRequest<TResponse>
    {
        bool CanHandle(TRequest request);
        TResponse Request(TRequest request);
    }
}