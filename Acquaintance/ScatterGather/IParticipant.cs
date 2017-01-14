namespace Acquaintance.ScatterGather
{
    public interface IParticipant<in TRequest, out TResponse>
    {
        bool CanHandle(TRequest request);
        IDispatchableScatter<TResponse> Scatter(TRequest request);
        bool ShouldStopParticipating { get; }
    }
}