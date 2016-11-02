namespace Acquaintance.RequestResponse
{
    public interface IListener<in TRequest, out TResponse>
    {
        bool CanHandle(TRequest request);
        IDispatchableRequest<TResponse> Request(TRequest request);
    }
}