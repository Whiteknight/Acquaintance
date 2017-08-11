using System;
using Acquaintance.Utility;

namespace Acquaintance.ScatterGather
{
    public class ScatterChannelProxy<TRequest, TResponse>
    {
        private readonly IScatterGatherBus _messageBus;

        public ScatterChannelProxy(IScatterGatherBus messageBus)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            _messageBus = messageBus;
        }

        public ScatterRequest<TResponse> Scatter(TRequest request)
        {
            return _messageBus.Scatter<TRequest, TResponse>(request);
        }

        public ScatterRequest<TResponse> Scatter(string topic, TRequest request)
        {
            return _messageBus.Scatter<TRequest, TResponse>(topic, request);
        }

        public IDisposable Participate(Action<ITopicParticipantBuilder<TRequest, TResponse>> build)
        {
            return _messageBus.Participate(build);
        }

        public IDisposable Participate(string topic, IParticipant<TRequest, TResponse> participant)
        {
            return _messageBus.Participate(topic, participant);
        }
    }
}
