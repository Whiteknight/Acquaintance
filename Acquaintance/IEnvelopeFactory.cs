using System.Collections.Generic;
using Acquaintance.Utility;

namespace Acquaintance
{
    public interface IEnvelopeFactory
    {
        Envelope<TPayload> Create<TPayload>(string[] topics, TPayload payload, IReadOnlyDictionary<string, string> metadata = null);
    }

    public static class EnvelopeFactoryExtensions
    {
        public static Envelope<TPayload> Create<TPayload>(this IEnvelopeFactory factory, string topic, TPayload payload, IReadOnlyDictionary<string, string> metadata = null)
        {
            var topics = TopicUtility.CanonicalizeTopics(topic);
            return factory.Create(topics, payload, metadata);
        }
    }
}