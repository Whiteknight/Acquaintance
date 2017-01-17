using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Acquaintance.ScatterGather
{
    public class ScatterGatherChannel<TRequest, TResponse> : IScatterGatherChannel<TRequest, TResponse>
    {
        private readonly ConcurrentDictionary<Guid, IParticipant<TRequest, TResponse>> _participants;

        public ScatterGatherChannel()
        {
            _participants = new ConcurrentDictionary<Guid, IParticipant<TRequest, TResponse>>();
            Id = Guid.NewGuid();
        }

        public Guid Id { get; }

        public IEnumerable<IDispatchableScatter<TResponse>> Scatter(TRequest request)
        {
            var waiters = new List<IDispatchableScatter<TResponse>>();
            var toRemove = new List<Guid>();
            foreach (var kvp in _participants)
            {
                try
                {
                    var participant = kvp.Value;
                    if (!participant.CanHandle(request))
                        continue;

                    // TODO: We should order these so worker thread requests are dispatched first, followed by
                    // immediate requests.
                    var responseWaiter = participant.Scatter(request);
                    if (participant.ShouldStopParticipating)
                        toRemove.Add(kvp.Key);
                    waiters.Add(responseWaiter);
                }
                catch (Exception e)
                {
                }
            }
            foreach (var id in toRemove)
                Unsubscribe(id);
            return waiters;
        }

        public SubscriptionToken Participate(IParticipant<TRequest, TResponse> listener)
        {
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));

            Guid id = Guid.NewGuid();
            _participants.TryAdd(id, listener);
            return new SubscriptionToken(this, id);
        }

        public void Unsubscribe(Guid id)
        {
            IParticipant<TRequest, TResponse> subscription;
            _participants.TryRemove(id, out subscription);
        }

        public void Dispose()
        {
            _participants.Clear();
        }
    }
}