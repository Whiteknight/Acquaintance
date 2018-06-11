using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Acquaintance
{
    public class EnvelopeHistory
    {
        public EnvelopeHistory(string originBusId, IReadOnlyList<EnvelopeHistoryHop> hops)
        {
            OriginBusId = originBusId;
            Hops = hops;
        }

        public string OriginBusId { get; }
        public IReadOnlyList<EnvelopeHistoryHop> Hops { get; }
    }

    public class EnvelopeHistoryHop
    {
        public EnvelopeHistoryHop(string busId, long envelopeId)
        {
            BusId = busId;
            EnvelopeId = envelopeId;
        }

        public string BusId { get; }
        public long EnvelopeId { get; }
    }

    public class EnvelopeHistoryParser
    {
        public EnvelopeHistory ParseHistory(string originBusId, string history)
        {
            if (string.IsNullOrEmpty(history))
                return new EnvelopeHistory(originBusId, new EnvelopeHistoryHop[0]);

            var hops = history.Split('\n')
                .Select(line =>
                {
                    if (line == null || line.Length < 3)
                        return null;
                    var p = line.Split(':');
                    if (p.Length < 2 || string.IsNullOrEmpty(p[0]))
                        return null;
                    if (!long.TryParse(p[1], out long messageId))
                        return null;
                    return new EnvelopeHistoryHop(p[0], messageId);
                })
                .Where(h => h != null)
                .ToList();
            return new EnvelopeHistory(originBusId, hops);
        }
    }

    public static class Envelope
    {
        public const string MetadataHistory = "_aq:history";

        // TODO: Need a parser for this data, so we can extract a strongly-typed itinerary from and existing message
        public static string CreateHistoryEntry(string messageBusId, long messageId)
        {
            return $"{messageBusId}:{messageId}";
        }

        public static string AppendHistoryEntry(string messageBusId, long messageId, string existingHistory)
        {
            var entry = CreateHistoryEntry(messageBusId, messageId);
            if (string.IsNullOrEmpty(existingHistory))
                return entry;
            return existingHistory + "\n" + entry;
        }
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
            return new Dictionary<string, string>(_metadata);
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
