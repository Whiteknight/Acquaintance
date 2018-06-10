using System.Collections.Generic;
using Acquaintance.Utility;

namespace Acquaintance
{
    /// <summary>
    /// Factory for creating Envelopes
    /// </summary>
    public interface IEnvelopeFactory
    {
        /// <summary>
        /// Create an envelope with the given fields set as immutable data
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="topics"></param>
        /// <param name="payload"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        Envelope<TPayload> Create<TPayload>(string[] topics, TPayload payload, IReadOnlyDictionary<string, string> metadata = null);

        /// <summary>
        /// Create an envelope to hold a message which has originated from a remote source
        /// For most cases you want to use .Create instead
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="originBusId"></param>
        /// <param name="topics"></param>
        /// <param name="payload"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        Envelope<TPayload> CreateFromRemote<TPayload>(string originBusId, string[] topics, TPayload payload, IReadOnlyDictionary<string, string> metadata = null);
    }

    public static class EnvelopeFactoryExtensions
    {
        /// <summary>
        /// Create an envelope for a single topic with the given fields set as immutable data
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="factory"></param>
        /// <param name="topic"></param>
        /// <param name="payload"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        public static Envelope<TPayload> Create<TPayload>(this IEnvelopeFactory factory, string topic, TPayload payload, IReadOnlyDictionary<string, string> metadata = null)
        {
            var topics = Topics.Canonicalize(topic);
            return factory.Create(topics, payload, metadata);
        }
    }
}