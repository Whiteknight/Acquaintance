using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Acquaintance.Logging;
using Acquaintance.Utility;

namespace Acquaintance.ScatterGather
{
    public class ScatterGatherChannel<TRequest, TResponse> : IScatterGatherChannel<TRequest, TResponse>
    {
        private readonly ILogger _log;
        private readonly ConcurrentDictionary<Guid, IParticipant<TRequest, TResponse>> _participants;

        public ScatterGatherChannel(ILogger log)
        {
            _log = log;
            _participants = new ConcurrentDictionary<Guid, IParticipant<TRequest, TResponse>>();
            Id = Guid.NewGuid();
        }

        public Guid Id { get; }

        public void Scatter(TRequest request, ScatterRequest<TResponse> scatter)
        {
            var toRemove = new List<Guid>();
            foreach (var kvp in _participants)
            {
                try
                {
                    var participant = kvp.Value;
                    if (!participant.CanHandle(request))
                        continue;

                    scatter.AddParticipant(participant.Id);
                    participant.Scatter(request, scatter);
                    if (participant.ShouldStopParticipating)
                        toRemove.Add(kvp.Key);
                }
                catch (Exception e)
                {
                    _log.Warn("Participant {0} threw exception {1}\n{2}", kvp.Key, e.Message, e.StackTrace);
                }
            }
            foreach (var id in toRemove)
                Unsubscribe(id);
        }

        public SubscriptionToken Participate(IParticipant<TRequest, TResponse> listener)
        {
            Assert.ArgumentNotNull(listener, nameof(listener));

            var id = Guid.NewGuid();
            _participants.TryAdd(id, listener);
            return new SubscriptionToken(this, id);
        }

        public void Unsubscribe(Guid id)
        {
            _participants.TryRemove(id, out IParticipant<TRequest, TResponse> subscription);
        }

        public void Dispose()
        {
            _participants.Clear();
        }
    }
}