using System;
using System.Linq;
using Acquaintance.Utility;

namespace Acquaintance.RequestResponse
{
    public class TrieListenerStore : IListenerStore, IDisposable
    {
        private readonly StringTrie<object> _listeners;

        public TrieListenerStore()
        {
            _listeners = new StringTrie<object>();
        }

        public IDisposable Listen<TRequest, TResponse>(string topic, IListener<TRequest, TResponse> listener)
        {
            listener.Id = Guid.NewGuid(); 
            var inserted = _listeners.GetOrInsert(typeof(TRequest).FullName, typeof(TResponse).FullName, topic.Split('.'), () => listener);
            if (inserted == null)
                throw new Exception("Could not get channel");
            if (inserted != listener)
                throw new Exception("Could not add a second listener to this channel");
            return new Token<TRequest, TResponse>(this, topic, listener.Id);
        }

        public IListener<TRequest, TResponse> GetListener<TRequest, TResponse>(string topic)
        {
            var allListeners = _listeners.Get(typeof(TRequest).FullName, typeof(TResponse).FullName, topic.Split('.'))
                .OfType< IListener<TRequest, TResponse>>()
                .ToArray();
            var toRemove = allListeners.Where(l => l.ShouldStopListening).ToArray();
            foreach (var removeListener in toRemove)
                RemoveListener<TRequest, TResponse>(topic, removeListener.Id);
            return allListeners.FirstOrDefault(l => !l.ShouldStopListening);
        }

        public void RemoveListener<TRequest, TResponse>(string topic, IListener<TRequest, TResponse> listener)
        {
            RemoveListener<TRequest, TResponse>(topic, listener.Id);
        }

        public void Dispose()
        {
            _listeners.OnEach(v => (v as IDisposable)?.Dispose());
        }

        private class Token<TRequest, TResponse> : IDisposable
        {
            private readonly TrieListenerStore _store;
            private readonly string _topic;
            private readonly Guid _id;

            public Token(TrieListenerStore store, string topic, Guid id)
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
            var root1 = typeof(TRequest).FullName;
            var root2 = typeof(TResponse).FullName;
            var path = topic.Split('.');
            var listenerObj = _listeners.Get(root1, root2, path).FirstOrDefault();
            var listener = listenerObj as IListener<TRequest, TResponse>;
            if (listener == null || listener.Id != id)
                return;
            _listeners.RemoveValue(root1, root2, path, v => (v as IDisposable)?.Dispose());
        }
    }
}