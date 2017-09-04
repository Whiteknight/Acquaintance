using System;

namespace Acquaintance.Routing
{
    public class PayloadExamineRule<T> : IRouteRule<T>
    {
        private readonly Func<T, string, string> _getRoute;

        public PayloadExamineRule(Func<T, string> getRoute)
        {
            _getRoute = (payload, topic) => getRoute(payload);
        }

        public PayloadExamineRule(Func<T, string, string> getRoute)
        {
            _getRoute = getRoute;
        }

        public string[] GetRoute(string topic, Envelope<T> envelope)
        {
            var newTopic = _getRoute?.Invoke(envelope.Payload, topic);
            return newTopic == null ? new string[0] : new[] { newTopic };
        }
    }
}
