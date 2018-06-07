using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.Utility
{
    public static class Topics
    {
        public static string[] Canonicalize(string[] topics)
        {
            if (topics == null || topics.Length == 0)
                return new[] { string.Empty };
            topics =  topics.Select(t => t ?? string.Empty).Distinct().ToArray();
            if (topics.Length == 0)
                return new[] { string.Empty };
            return topics;
        }

        public static string[] Canonicalize(IEnumerable<string> topics)
        {
            return Canonicalize(topics?.ToArray());
        }

        public static string[] Canonicalize(string topic)
        {
            return new[] { topic ?? string.Empty };
        }
    }
}
