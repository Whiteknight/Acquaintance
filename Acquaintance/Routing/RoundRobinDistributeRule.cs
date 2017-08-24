using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Acquaintance.Utility;

namespace Acquaintance.Routing
{
    public class RoundRobinDistributeRule<T> : IPublishRouteRule<T>, IRequestRouteRule<T>
    {
        private readonly string[] _topics;
        private volatile int _idx;

        public RoundRobinDistributeRule(IEnumerable<string> topics)
        {
            Assert.ArgumentNotNull(topics, nameof(topics));

            _topics = topics.ToArray();
            if (_topics.Length == 0)
                throw new Exception("No channel names provided");
            _idx = 0;
        }

        string[] IPublishRouteRule<T>.GetRoute(string topic, Envelope<T> envelope)
        {
            var newTopic = GetRouteInternal();
            return new[] { newTopic };
        }

        string IRequestRouteRule<T>.GetRoute(string topic, Envelope<T> envelope)
        {
            return GetRouteInternal();
        }

        private string GetRouteInternal()
        {
            int idx = Interlocked.Increment(ref _idx);
            idx = idx % _topics.Length;
            return _topics[idx];
        }
    }
}
