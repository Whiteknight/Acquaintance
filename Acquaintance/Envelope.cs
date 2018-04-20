using System;
using System.Collections.Concurrent;
using System.Threading;
using Acquaintance.Utility;

namespace Acquaintance
{
    public class Envelope<TPayload>
    {
        private ConcurrentDictionary<string, string> _metadata;

        public Envelope(Guid originBusId, long id, string[] topics, TPayload payload)
        {
            OriginBusId = originBusId;
            Id = id;
            Topics = TopicUtility.CanonicalizeTopics(topics);
            Payload = payload;
        }

        public Envelope(Guid originBusId, long id, string topic, TPayload payload)
        {
            OriginBusId = originBusId;
            Id = id;
            Topics = TopicUtility.CanonicalizeTopics(topic);
            Payload = payload;
        }

        public string[] Topics { get; }
        public TPayload Payload { get; }
        public Guid OriginBusId { get; }
        public long Id { get; }

        public Envelope<TPayload> RedirectToTopic(string topic)
        {
            var topics = TopicUtility.CanonicalizeTopics(topic);
            return RedirectToTopicsInternal(topics);
        }

        public Envelope<TPayload> RedirectToTopics(string[] topics)
        {
            topics = TopicUtility.CanonicalizeTopics(topics);
            return RedirectToTopicsInternal(topics);
        }

        private Envelope<TPayload> RedirectToTopicsInternal(string[] topics)
        {
            var envelope = new Envelope<TPayload>(OriginBusId, Id, topics, Payload);
            if (_metadata != null)
                envelope._metadata = new ConcurrentDictionary<string, string>(_metadata);
            return envelope;
        }

        public string GetMetadata(string name)
        {
            if (_metadata == null)
                return null;
            return _metadata.TryGetValue(name, out string value) ? value : null;
        }

        public void SetMetadata(string name, string value)
        {
            var metadata = _metadata;
            if (metadata == null)
            {
                var newMetadata = new ConcurrentDictionary<string, string>();
                var oldMetadata = Interlocked.CompareExchange(ref _metadata, newMetadata, null);
                metadata = oldMetadata == null ? newMetadata : _metadata;
            }

            metadata.AddOrUpdate(name, value, (a, b) => value);
        }
    }
}
