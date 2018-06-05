﻿using System;
using System.Collections.Generic;

namespace Acquaintance.RabbitMq
{
    public class RabbitEnvelope<TPayload>
    {
        public string RabbitTopic { get; set; }
        public string[] Topics { get; set; }
        public TPayload Payload { get; set; }
        public string OriginBusId { get; set; }
        public long Id { get; set; }
        public Dictionary<string, string> Metadata { get; set; }

        public Envelope<TPayload> ToLocalEnvelope()
        {
            var envelope = new Envelope<TPayload>(OriginBusId, Id, Topics, Payload);
            foreach (var kvp in Metadata ?? new Dictionary<string, string>())
                envelope.SetMetadata(kvp.Key, kvp.Value);
            return envelope;
        }

        public static RabbitEnvelope<TPayload> WrapForRabbit(string rabbitTopic, Envelope<TPayload> envelope)
        {
            return new RabbitEnvelope<TPayload>
            {
                RabbitTopic = rabbitTopic,
                Id = envelope.Id,
                Payload = envelope.Payload,
                OriginBusId = envelope.OriginBusId,
                Topics = envelope.Topics,
                Metadata = envelope.ExportMetadata()
            };
        }
    }
}