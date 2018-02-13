using System;
using System.Collections.Generic;
using System.Threading;

namespace Acquaintance.Utility
{
    public class EnvelopeFactory : IEnvelopeFactory
    {
        private readonly Guid _originBusId;
        private long _id;

        public EnvelopeFactory(Guid originBusId, long startId = 0)
        {
            _originBusId = originBusId;
            _id = startId;
        }

        public Envelope<TPayload> Create<TPayload>(string topic, TPayload payload, IReadOnlyDictionary<string, string> metadata = null)
        {
            long id = Interlocked.Increment(ref _id);
            var envelope = new Envelope<TPayload>(_originBusId, id, topic, payload);
            if (metadata != null)
            {
                foreach (var kvp in metadata)
                    envelope.SetMetadata(kvp.Key, kvp.Value);
            }
            return envelope;
        }
    }
}
