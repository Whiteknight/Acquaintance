using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Acquaintance
{
    public class Envelope<TPayload>
    {
        private volatile ConcurrentDictionary<string, string> _metadata;

        public Envelope(string originBusId, long id, string[] topics, TPayload payload)
        {
            OriginBusId = originBusId;
            Id = id;
            Topics = Utility.Topics.Canonicalize(topics);
            Payload = payload;
        }

        public Envelope(string originBusId, long id, string topic, TPayload payload)
        {
            OriginBusId = originBusId;
            Id = id;
            Topics = Utility.Topics.Canonicalize(topic);
            Payload = payload;
        }

        public string[] Topics { get; }
        public TPayload Payload { get; }
        public string OriginBusId { get; }
        public long Id { get; }

        public Envelope<TPayload> RedirectToTopic(string topic)
        {
            var topics = Utility.Topics.Canonicalize(topic);
            return RedirectToTopicsInternal(topics);
        }

        public Envelope<TPayload> RedirectToTopics(string[] topics)
        {
            topics = Utility.Topics.Canonicalize(topics);
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

        public Dictionary<string, string> ExportMetadata()
        {
            if (_metadata == null)
                return null;
            return new Dictionary<string, string>(_metadata);
        }

        public void SetMetadata(string name, string value)
        {
            EnsureMetadataExists().AddOrUpdate(name, value, (a, b) => value);
        }

        public void AppendMetadata(string name, string value, string separator = "\n")
        {
            EnsureMetadataExists().AddOrUpdate(name, value, (a, b) => a + separator + b);
        }

        public string GetAndClearMetadata(string name)
        {
            if (_metadata == null)
                return null;
            return _metadata.TryRemove(name, out string value) ? value : null;
        }

        private ConcurrentDictionary<string, string> EnsureMetadataExists()
        {
            if (_metadata == null)
            {
                var newMetadata = new ConcurrentDictionary<string, string>();
                var oldMetadata = Interlocked.CompareExchange(ref _metadata, newMetadata, null);
            }
            return _metadata;
        }
    }
}
