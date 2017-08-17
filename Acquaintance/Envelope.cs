﻿using System.Collections.Concurrent;
using System.Threading;

namespace Acquaintance
{
    public class Envelope<TPayload>
    {
        private ConcurrentDictionary<string, string> _metadata;

        public Envelope(long id, string topic, TPayload payload)
        {
            Id = id;
            Topic = topic ?? string.Empty;
            Payload = payload;
        }

        public string Topic { get; }
        public TPayload Payload { get; }
        public long Id { get; }

        public Envelope<TPayload> RedirectToTopic(string topic)
        {
            var envelope = new Envelope<TPayload>(Id, topic, Payload);
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
