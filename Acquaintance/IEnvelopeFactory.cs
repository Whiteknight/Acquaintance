using System;
using System.Collections.Generic;

namespace Acquaintance
{

    public interface IEnvelopeFactory
    {
        Envelope<TPayload> Create<TPayload>(string channel, TPayload payload, IReadOnlyDictionary<string, string> metadata = null);
    }
}