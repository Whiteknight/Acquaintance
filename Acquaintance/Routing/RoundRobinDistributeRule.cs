using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Acquaintance.Utility;

namespace Acquaintance.Routing
{
    public class RoundRobinDistributeRule<T> : IRouteRule<T>
    {
        private readonly string[] _topics;
        private int _idx;

        public RoundRobinDistributeRule(IEnumerable<string> topics)
        {
            Assert.ArgumentNotNull(topics, nameof(topics));

            _topics = topics.ToArray();
            if (_topics.Length == 0)
                throw new Exception("No channel names provided");
            _idx = 0;
        }

        string[] IRouteRule<T>.GetRoute(string topic, Envelope<T> envelope)
        {
            int idx = Interlocked.Increment(ref _idx);
            idx = idx % _topics.Length;
            var newTopic = _topics[idx];
            return new[] { newTopic };
        }
    }
}
