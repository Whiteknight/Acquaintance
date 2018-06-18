using System;
using Acquaintance.Logging;
using Acquaintance.Utility;

namespace Acquaintance.ScatterGather
{
    public class ParticipantDispatcher : IDisposable
    {
        private readonly ILogger _log;
        private readonly IParticipantStore _store;

        public ParticipantDispatcher(ILogger log, bool allowWildcards)
        {
            _log = log;
            _store = CreateStore(allowWildcards);
        }

        public IDisposable Participate<TRequest, TResponse>(string topic, IParticipant<TRequest, TResponse> participant)
        {
            Assert.ArgumentNotNull(participant, nameof(participant));
            var token = _store.Participate(topic ?? string.Empty, participant);
            _log.Debug("Participant {0} on RequestType={1} ResponseType={2} Topic={3}, with Id={4}", participant.Id, typeof(TRequest).FullName, typeof(TResponse).FullName, topic, participant.Id);
            return token;
        }

        public void Scatter<TRequest, TResponse>(string topic, Envelope<TRequest> envelope, IGatherReceiver<TResponse> scatter)
        {
            Assert.ArgumentNotNull(envelope, nameof(envelope));
            Assert.ArgumentNotNull(scatter, nameof(scatter));

            var topicEnvelope = envelope.RedirectToTopic(topic);
            var participants = _store.GetParticipants<TRequest, TResponse>(topic);
            _log.Debug("Scattering RequestType={0} ResponseType={1} Topic={2}", typeof(TRequest).FullName, typeof(TResponse).FullName, topic);
            foreach (var participant in participants)
            {
                try
                {
                    scatter.AddParticipant(participant.Id);
                    participant.Scatter(topicEnvelope, scatter);
                }
                catch (Exception e)
                {
                    _log.Error($"Unhandled scatter request Type={typeof(TRequest).FullName}, {typeof(TResponse).FullName} Topic={topic}: {e.Message}\n{e.StackTrace}");
                }
                if (participant.ShouldStopParticipating)
                    _store.RemoveParticipant(topic, participant);
            }
        }

        public void Dispose()
        {
            ObjectManagement.TryDispose(_store);
        }

        private static IParticipantStore CreateStore(bool allowWildcards)
        {
            if (allowWildcards)
                return new TrieParticipantStore();
            return new SimpleParticipantStore();
        }
    }
}