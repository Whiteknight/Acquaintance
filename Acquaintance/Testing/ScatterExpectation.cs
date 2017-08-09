using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Acquaintance.Testing
{
    public class ScatterExpectation<TRequest, TResponse> : IExpectation
    {
        private readonly string _channelName;
        private readonly string _description;
        private readonly Func<TRequest, bool> _filter;
        private Func<TRequest, IEnumerable<TResponse>> _getResponse;
        private readonly List<Action<TRequest, IEnumerable<TResponse>>> _actions;

        public ScatterExpectation(string channelName, string description, Func<TRequest, bool> filter)
        {
            _channelName = channelName;
            _description = description;
            _filter = filter;
            _actions = new List<Action<TRequest, IEnumerable<TResponse>>>();
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append("Scatter of type ");
            builder.Append(typeof(TRequest).FullName);
            builder.Append(" Gather of type ");
            builder.Append(typeof(TResponse).FullName);
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

        public IEnumerable<TResponse> TryHandle(TRequest request)
        {
            var response = GetResponse(request);

            foreach (var act in _actions)
                act(request, response);
            return response;
        }

        private IEnumerable<TResponse> GetResponse(TRequest request)
        {
            if (_filter != null && !_filter(request))
                return Enumerable.Empty<TResponse>();

            IsMet = true;

            if (_getResponse == null)
                return Enumerable.Empty<TResponse>();
            return _getResponse(request);
        }

        public ScatterExpectation<TRequest, TResponse> WillReturn(TResponse response)
        {
            _getResponse = r => new[] { response };
            return this;
        }

        public ScatterExpectation<TRequest, TResponse> WillReturn(IEnumerable<TResponse> response)
        {
            _getResponse = r => response;
            return this;
        }

        public ScatterExpectation<TRequest, TResponse> WillReturn(Func<TRequest, TResponse> getResponse)
        {
            _getResponse = r => new[] { getResponse(r) };
            return this;
        }

        public ScatterExpectation<TRequest, TResponse> WillReturn(Func<TRequest, IEnumerable<TResponse>> getResponse)
        {
            _getResponse = getResponse;
            return this;
        }

        public ScatterExpectation<TRequest, TResponse> Callback(Action<TRequest, IEnumerable<TResponse>> callback)
        {
            _actions.Add(callback);
            return this;
        }
    }
}