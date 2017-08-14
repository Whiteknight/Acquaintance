using Acquaintance.ScatterGather;
using System;

namespace Acquaintance
{
    public static class ScatterGatherMessageBusExtensions
    {
        public static IScatter<TResponse> Scatter<TRequest, TResponse>(this IScatterGatherBus messageBus, TRequest request)
        {
            return messageBus.Scatter<TRequest, TResponse>(string.Empty, request);
        }

        public static IDisposable Participate<TRequest, TResponse>(this IScatterGatherBus messageBus, Action<ITopicParticipantBuilder<TRequest, TResponse>> build)
        {
            var builder = new ParticipantBuilder<TRequest, TResponse>(messageBus, messageBus.ThreadPool);
            build(builder);
            var participant = builder.BuildParticipant();
            var token = messageBus.Participate(builder.Topic, participant);
            return builder.WrapToken(token);
        }

        public static ScatterChannelProxy<TRequest, TResponse> GetScatterChannel<TRequest, TResponse>(this IScatterGatherBus messageBus)
        {
            return new ScatterChannelProxy<TRequest, TResponse>(messageBus);
        }
    }
}