using System;
using System.Collections.Generic;
using System.Threading;

namespace Acquaintance.Utility
{
    public class EnvelopeFactory : IEnvelopeFactory
    {
        private readonly string _originBusId;
        private long _id;

        public EnvelopeFactory(string originBusId, long startId = 0)
        {
            _originBusId = originBusId;
            _id = startId;
        }

        public Envelope<TPayload> Create<TPayload>(string[] topics, TPayload payload, IReadOnlyDictionary<string, string> metadata = null)
        {
            long id = Interlocked.Increment(ref _id);
            var envelope = new Envelope<TPayload>(_originBusId, id, topics, payload);
            SetMetadata(metadata, envelope);
            return envelope;
        }

        private static void SetMetadata<TPayload>(IReadOnlyDictionary<string, string> metadata, Envelope<TPayload> envelope)
        {
            if (metadata == null)
                return;
            foreach (var kvp in metadata)
                envelope.SetMetadata(kvp.Key, kvp.Value);
        }
    }
}
