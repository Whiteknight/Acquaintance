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
            topics =  topics.Select(t => t ?? string.Empty).Distinct().ToArray();
            if (topics.Length == 0)
                return new[] { string.Empty };
            return topics;
        }

        public static string[] CanonicalizeTopics(IEnumerable<string> topics)
        {
            return CanonicalizeTopics(topics?.ToArray());
        }

        public static string[] CanonicalizeTopics(string topic)
        {
            return new[] { topic ?? string.Empty };
        }
    }
}
