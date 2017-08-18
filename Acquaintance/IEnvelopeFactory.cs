using System.Collections.Generic;

namespace Acquaintance
{
    public interface IEnvelopeFactory
    {
        Envelope<TPayload> Create<TPayload>(string topic, TPayload payload, IReadOnlyDictionary<string, string> metadata = null);
    }
}