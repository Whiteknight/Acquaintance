using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Acquaintance.Messages;

namespace Acquaintance
{
    public static class Envelope
    {
        public const string MetadataInternalPrefix = "_aq";
        public const string MetadataHistory = "_aq:history";

        
    }

    public class Envelope<TPayload>
    {
        private ConcurrentDictionary<string, string> _metadata;

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
            var metadata = GetMetadataCollection();
            if (metadata != null)
                envelope._metadata = new ConcurrentDictionary<string, string>(_metadata);
            return envelope;
        }

        public string GetMetadata(string name)
        {
            var metadata = GetMetadataCollection();
            if (metadata == null)
                return null;
            return metadata.TryGetValue(name, out string value) ? value : null;
        }

        public Dictionary<string, string> ExportMetadata()
        {
            var metadata = GetMetadataCollection();
            if (metadata == null)
                return new Dictionary<string, string>();
            return metadata
                .ToArray()
                .Where(kvp => !kvp.Key.StartsWith(Envelope.MetadataInternalPrefix))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public void SetMetadata(string name, string value)
        {
            EnsureMetadataCollectionExists().AddOrUpdate(name, value, (a, b) => value);
        }

        public void AppendMetadata(string name, string value, string separator = "\n")
        {
            EnsureMetadataCollectionExists().AddOrUpdate(name, value, (a, existing) => existing + separator + value);
        }

        public string GetAndClearMetadata(string name)
        {
            var metadata = GetMetadataCollection();
            if (metadata == null)
                return null;
            return metadata.TryRemove(name, out string value) ? value : null;
        }

        public EnvelopeHistory GetHistory()
        {
            var history = GetMetadata(Envelope.MetadataHistory);
            return new EnvelopeHistoryParser().ParseHistory(OriginBusId, history);
        }

        private ConcurrentDictionary<string, string> EnsureMetadataCollectionExists()
        {
            var metadata = Interlocked.CompareExchange(ref _metadata, null, null);
            if (metadata != null)
                return metadata;

            var newMetadata = new ConcurrentDictionary<string, string>();
            var oldMetadata = Interlocked.CompareExchange(ref _metadata, newMetadata, null);
            if (oldMetadata == null)
                return newMetadata;
            
            return Interlocked.CompareExchange(ref _metadata, null, null);
        }

        private ConcurrentDictionary<string, string> GetMetadataCollection()
        {
            return Interlocked.CompareExchange(ref _metadata, null, null);
        }
    }
}
