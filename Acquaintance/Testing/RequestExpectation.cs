using System;
using System.Collections.Generic;
using System.Text;

namespace Acquaintance.Testing
{
    public class RequestExpectation<TRequest, TResponse> : IExpectation
    {
        private readonly string _topic;
        private readonly string _description;
        private readonly Func<TRequest, bool> _filter;
        private readonly List<Action<TRequest, TResponse>> _actions;

        private Func<TRequest, TResponse> _getResponse;

        public RequestExpectation(string topic, string description, Func<TRequest, bool> filter)
        {
            _topic = topic;
            _description = description;
            _filter = filter;
            _actions = new List<Action<TRequest, TResponse>>();
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append("Request of type ");
            builder.Append(typeof(TRequest).FullName);
            builder.Append(" Response of type ");
            builder.Append(typeof(TResponse).FullName);
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

        public TResponse TryHandle(TRequest request)
        {
            var response = GetResponse(request);

            foreach (var act in _actions)
                act(request, response);

            return response;
        }

        private TResponse GetResponse(TRequest request)
        {
            if (_filter != null && !_filter(request))
                return default(TResponse);

            IsMet = true;

            if (_getResponse == null)
                return default(TResponse);
            return _getResponse(request);
        }

        public RequestExpectation<TRequest, TResponse> WillReturn(TResponse response)
        {
            _getResponse = r => response;
            return this;
        }

        public RequestExpectation<TRequest, TResponse> WillReturn(Func<TRequest, TResponse> getResponse)
        {
            _getResponse = getResponse;
            return this;
        }

        public RequestExpectation<TRequest, TResponse> WillThrow(Exception e)
        {
            _actions.Add((q, s) => throw e);
            return this;
        }

        public RequestExpectation<TRequest, TResponse> Callback(Action<TRequest, TResponse> callback)
        {
            _actions.Add(callback);
            return this;
        }
    }
}