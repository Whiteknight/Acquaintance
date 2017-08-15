using System.Collections.Generic;

namespace Acquaintance
{
    public class Envelope<TPayload>
    {
        private IDictionary<string, string> _metadata;

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
                envelope._metadata = new Dictionary<string, string>(_metadata);
            return envelope;
        }

        public string GetMetadata(string name)
        {
            if (_metadata == null)
                return null;
            return _metadata.ContainsKey(name) ? _metadata[name] : null;
        }

        public void SetMetadata(string name, string value)
        {
            if (_metadata == null)
                _metadata = new Dictionary<string, string>();

            if (_metadata.ContainsKey(name))
                _metadata[name] = value;
            else
                _metadata.Add(name, value);
        }
    }
}
