using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.Utility
{
    public static class TopicUtility
    {
        public static string[] CanonicalizeTopics(string[] topics)
        {
            if (topics == null || topics.Length == 0)
                return new[] { string.Empty };
            return topics.Select(t => t ?? string.Empty).Distinct().ToArray();
        }

        public static string[] CanonicalizeTopics(IEnumerable<string> topics)
        {
            if (topics == null)
                return new[] { string.Empty };
            return CanonicalizeTopics(topics.ToArray());
        }

        public static string[] CanonicalizeTopics(string topic)
        {
            return new[] { topic ?? string.Empty };
        }
    }
}
