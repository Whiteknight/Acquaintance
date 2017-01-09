using Acquaintance.ScatterGather;
using Acquaintance.Utility;
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
            var listeners = builder.BuildParticipants();
            if (listeners.Count == 1)
                return messageBus.Participate(builder.ChannelName, listeners[0]);

            var tokens = new DisposableCollection();
            foreach (var listener in listeners)
            {
                var token = messageBus.Participate(builder.ChannelName, listener);
                tokens.Add(token);
            }
            return tokens;
        }
    }
}