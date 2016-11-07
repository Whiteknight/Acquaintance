using System;
using System.Text;

namespace Acquaintance.Testing
{
    public class PublishExpectation<TPayload> : IExpectation
    {
        private readonly string _channelName;
        private readonly string _description;
        private readonly Func<TPayload, bool> _filter;

        public PublishExpectation(string channelName, string description, Func<TPayload, bool> filter)
        {
            _channelName = channelName;
            _description = description;
            _filter = filter;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Publish of type ");
            builder.Append(typeof(TPayload).FullName);
            builder.AppendFormat(" on channel '{0}'", _channelName ?? string.Empty);
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
        }
    }
}