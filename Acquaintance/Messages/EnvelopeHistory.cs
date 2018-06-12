using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.Messages
{
    public class EnvelopeHistory
    {
        private readonly List<EnvelopeHistoryHop> _hops;

        public EnvelopeHistory(string originBusId, List<EnvelopeHistoryHop> hops)
        {
            OriginBusId = originBusId;
            _hops = hops;
        }

        public string OriginBusId { get; }
        public IReadOnlyList<EnvelopeHistoryHop> Hops => _hops;

        public void AddHop(string messageBusId, long envelopeId)
        {
            _hops.Add(new EnvelopeHistoryHop(messageBusId, envelopeId));
        }

        public override string ToString()
        {
            var strings = _hops.Select(h => $"{h.BusId}:{h.EnvelopeId}");
            return string.Join("\n", strings);
        }
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
}
