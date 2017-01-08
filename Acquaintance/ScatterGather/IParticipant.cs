namespace Acquaintance.ScatterGather
{
    public interface IParticipant<in TRequest, out TResponse>
    {
        bool CanHandle(TRequest request);
        IDispatchableRequest<TResponse> Request(TRequest request);
        bool ShouldStopParticipating { get; }
    }
}