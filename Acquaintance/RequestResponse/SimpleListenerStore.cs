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
            var key = GetReqResKey(typeof(TRequest), typeof(TResponse), topic);
            if (_listeners.ContainsKey(key))
                throw new Exception("Cannot add a second listener to this channel");
            var id = Guid.NewGuid();
            listener.Id = id;
            if (!_listeners.TryAdd(key, listener))
                throw new Exception("Cannot add a second listener to this channel");
            return new Token<TRequest, TResponse>(this, topic, id);
        }

        public IListener<TRequest, TResponse> GetListener<TRequest, TResponse>(string topic)
        {
            var key = GetReqResKey(typeof(TRequest), typeof(TResponse), topic);
            if (!_listeners.TryGetValue(key, out object listenerObj) || listenerObj == null)
                return null;
            var listener = listenerObj as IListener<TRequest, TResponse>;
            if (listener == null)
                throw new Exception($"Wrong listener type. Expected {typeof(IListener<TRequest, TResponse>).FullName} but got {listenerObj.GetType().FullName}");
            return listener;
        }

        private class Token<TRequest, TResponse> : IDisposable
        {
            private readonly SimpleListenerStore _store;
            private readonly string _topic;
            private readonly Guid _id;

            public Token(SimpleListenerStore store, string topic, Guid id)
            {
                _store = store;
                _topic = topic;
                _id = id;
            }

            public void Dispose()
            {
                _store.RemoveListener<TRequest, TResponse>(_topic, _id);
            }
        }

        private void RemoveListener<TRequest, TResponse>(string topic, Guid id)
        {
            var key = GetReqResKey(typeof(TRequest), typeof(TResponse), topic);
            var found = _listeners.TryGetValue(key, out object listenerObj);
            if (!found || listenerObj == null)
                return;
            var listener = listenerObj as IListener<TRequest, TResponse>;
            if (listener == null || listener.Id != id)
                return;
            _listeners.TryRemove(key, out object whatever);
        }

        private static string GetReqResKey(Type requestType, Type responseType, string name)
        {
            return $"Request={requestType.AssemblyQualifiedName}:Response={responseType.AssemblyQualifiedName}:Name={name ?? string.Empty}";
        }
    }
}