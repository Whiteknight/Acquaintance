using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.Messages
{
    public class EnvelopeHistoryParser
    {
        public EnvelopeHistory ParseHistory(string originBusId, string history)
        {
            if (string.IsNullOrEmpty(history))
                return new EnvelopeHistory(originBusId, new List<EnvelopeHistoryHop>());

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
}