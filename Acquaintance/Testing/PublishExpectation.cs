using System;
using System.Collections.Generic;
using System.Text;

namespace Acquaintance.Testing
{
    public class PublishExpectation<TPayload> : IExpectation
    {
        private readonly string _topic;
        private readonly string _description;
        private readonly Func<TPayload, bool> _filter;
        private readonly List<Action<TPayload>> _actions;

        public PublishExpectation(string topic, string description, Func<TPayload, bool> filter)
        {
            _topic = topic;
            _description = description;
            _filter = filter;
            _actions = new List<Action<TPayload>>();
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append("Publish of type ");
            builder.Append(typeof(TPayload).FullName);
            builder.AppendFormat(" on topic '{0}'", _topic ?? string.Empty);
            if (!string.IsNullOrEmpty(_description))
            {
                builder.Append(": ");
                builder.Append(_description);
            }
            else
                builder.Append(_filter != null ? " (filtered)" : " (unfiltered)");
            return builder.ToString();
        }

        public bool IsMet { get; private set; }

        public void TryReceive(TPayload payload)
        {
            if (_filter != null && !_filter(payload))
                return;
            IsMet = true;

            foreach (var action in _actions)
                action(payload);
        }

        public PublishExpectation<TPayload> Callback(Action<TPayload> act)
        {
            _actions.Add(act);
            return this;
        }
    }
}