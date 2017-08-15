using System;

namespace Acquaintance.ScatterGather
{
    public interface IScatterGatherChannel<TRequest, TResponse> : IScatterGatherChannel
    {
        void Scatter(Envelope<TRequest> request, Scatter<TResponse> scatter);
        SubscriptionToken Participate(IParticipant<TRequest, TResponse> participant);
    }

    public interface IScatterGatherChannel : IChannel
    {
        Guid Id { get; }
    }
}