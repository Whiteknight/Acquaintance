using System.Collections.Generic;
using System.Threading;

namespace Acquaintance.Utility
{
    public class EnvelopeFactory : IEnvelopeFactory
    {
        private readonly string _originBusId;
        private readonly IIdGenerator _idGenerator;


        public EnvelopeFactory(string originBusId, IIdGenerator idGenerator)
        {
            Assert.ArgumentNotNull(idGenerator, nameof(idGenerator));

            _originBusId = originBusId;
            _idGenerator = idGenerator;
        }

        public Envelope<TPayload> Create<TPayload>(string[] topics, TPayload payload, IReadOnlyDictionary<string, string> metadata = null)
        {
            long id = _idGenerator.GenerateNext();
            var envelope = new Envelope<TPayload>(_originBusId, id, topics, payload);
            SetMetadata(metadata, envelope);
            return envelope;
        }

        public Envelope<TPayload> CreateFromRemote<TPayload>(string originBusId, string[] topics, TPayload payload, IReadOnlyDictionary<string, string> metadata = null)
        {
            long id = _idGenerator.GenerateNext();
            var envelope = new Envelope<TPayload>(originBusId, id, topics, payload);
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
