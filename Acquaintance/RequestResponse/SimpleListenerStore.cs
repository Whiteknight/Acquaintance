using System;
using System.Collections.Concurrent;

namespace Acquaintance.RequestResponse
{
    public class SimpleListenerStore : IListenerStore
    {
        private readonly ConcurrentDictionary<string, object> _listeners;

        public SimpleListenerStore()
        {
            _listeners = new ConcurrentDictionary<string, object>();
        }

        public IDisposable Listen<TRequest, TResponse>(string topic, IListener<TRequest, TResponse> listener)
        {
            var key = GetKey(typeof(TRequest), typeof(TResponse), topic);
            if (_listeners.ContainsKey(key))
                throw new Exception("Cannot add a second listener to this channel");
            var id = Guid.NewGuid();
            listener.Id = id;
            if (!_listeners.TryAdd(key, listener))
                throw new Exception("Cannot add a second listener to this channel");
            return new Token<TRequest, TResponse>(this, typeof(TRequest).Name, typeof(TResponse).Name, topic, id);
        }

        public IListener<TRequest, TResponse> GetListener<TRequest, TResponse>(string topic)
        {
            var key = GetKey(typeof(TRequest), typeof(TResponse), topic);
            if (!_listeners.TryGetValue(key, out object listenerObj) || listenerObj == null)
                return null;
            if (!(listenerObj is IListener<TRequest, TResponse> listener))
                throw new Exception($"Wrong listener type. Expected {typeof(IListener<TRequest, TResponse>).FullName} but got {listenerObj.GetType().FullName}");
            if (listener.ShouldStopListening)
            {
                RemoveListener<TRequest, TResponse>(topic, listener.Id);
                return null;
            }
            return listener;
        }

        public void RemoveListener<TRequest, TResponse>(string topic, IListener<TRequest, TResponse> listener)
        {
            RemoveListener<TRequest, TResponse>(topic, listener.Id);
        }

        private class Token<TRequest, TResponse> : IDisposable
        {
            private readonly SimpleListenerStore _store;
            private readonly string _requestType;
            private readonly string _responseType;
            private readonly string _topic;
            private readonly Guid _id;

            public Token(SimpleListenerStore store, string requestType, string responseType, string topic, Guid id)
            {
                _store = store;
                _requestType = requestType;
                _responseType = responseType;
                _topic = topic;
                _id = id;
            }

            public void Dispose()
            {
                _store.RemoveListener<TRequest, TResponse>(_topic, _id);
            }

            public override string ToString()
            {
                return $"Listener Request={_requestType} Response={_responseType} Topic={_topic} Id={_id}";
            }
        }

        private void RemoveListener<TRequest, TResponse>(string topic, Guid id)
        {
            var key = GetKey(typeof(TRequest), typeof(TResponse), topic);
            var found = _listeners.TryGetValue(key, out object listenerObj);
            if (!found || listenerObj == null)
                return;
            if (!(listenerObj is IListener<TRequest, TResponse> listener) || listener.Id != id)
                return;
            _listeners.TryRemove(key, out object whatever);
        }

        private static string GetKey(Type requestType, Type responseType, string name)
        {
            return $"Request={requestType.AssemblyQualifiedName}:Response={responseType.AssemblyQualifiedName}:Name={name ?? string.Empty}";
        }
    }
}