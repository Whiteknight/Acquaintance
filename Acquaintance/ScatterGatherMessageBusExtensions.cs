using Acquaintance.ScatterGather;
using System;

namespace Acquaintance
{
    public static class ScatterGatherMessageBusExtensions
    {
        public static IGatheredResponse<TResponse> Scatter<TRequest, TResponse>(this IRequestable messageBus, TRequest request)
        {
            return messageBus.Scatter<TRequest, TResponse>(string.Empty, request);
        }

        public static IDisposable Participate<TRequest, TResponse>(this IReqResBus messageBus, Action<ParticipantBuilder<TRequest, TResponse>> build)
        {
            var builder = new ParticipantBuilder<TRequest, TResponse>(messageBus, messageBus.ThreadPool);
            build(builder);
            var participant = builder.BuildParticipant();
            return messageBus.Participate(builder.ChannelName, participant);
        }
    }
}