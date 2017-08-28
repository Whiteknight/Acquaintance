using System.Collections.Generic;
using System.Threading;

namespace Acquaintance.Utility
{
    public class EnvelopeFactory : IEnvelopeFactory
    {
        private long _id;

        public EnvelopeFactory(long startId = 0)
        {
            _id = startId;
        }

        public Envelope<TPayload> Create<TPayload>(string topic, TPayload payload, IReadOnlyDictionary<string, string> metadata = null)
        {
            long id = Interlocked.Increment(ref _id);
            var envelope = new Envelope<TPayload>(id, topic, payload);
            if (metadata != null)
            {
                foreach (var kvp in metadata)
                    envelope.SetMetadata(kvp.Key, kvp.Value);
            }
            return envelope;
        }
    }
}
