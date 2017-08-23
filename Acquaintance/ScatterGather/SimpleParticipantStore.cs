using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.ScatterGather
{
    public class SimpleParticipantStore : IParticipantStore
    {
        private readonly ConcurrentDictionary<string, object> _channels;

        public SimpleParticipantStore()
        {
            _channels = new ConcurrentDictionary<string, object>();
        }

        public IDisposable Participate<TRequest, TResponse>(string topic, IParticipant<TRequest, TResponse> participant)
        {
            var key = GetKey(typeof(TRequest), typeof(TResponse), topic);
            var channelObj = _channels.GetOrAdd(key, s => new Channel<TRequest, TResponse>());
            var channel = channelObj as Channel<TRequest, TResponse>;
            if (channel == null)
                throw new Exception($"Incorrect Channel type. Expected {typeof(Channel<TRequest, TResponse>)} but found {channelObj.GetType().FullName}");
            participant.Id = Guid.NewGuid();
            channel.Add(participant);
            return new Token<TRequest, TResponse>(this, topic, participant.Id);
        }

        public IEnumerable<IParticipant<TRequest, TResponse>> GetParticipants<TRequest, TResponse>(string topic)
        {
            var key = GetKey(typeof(TRequest), typeof(TResponse), topic);
            if (!_channels.TryGetValue(key, out object channelObj))
                return Enumerable.Empty<IParticipant<TRequest, TResponse>>();
            var channel = channelObj as Channel<TRequest, TResponse>;
            if (channel == null)
                throw new Exception($"Incorrect Channel type. Expected {typeof(Channel<TRequest, TResponse>)} but found {channelObj.GetType().FullName}");
            return channel.GetAll();
        }

        public void RemoveParticipant<TRequest, TResponse>(string topic, IParticipant<TRequest, TResponse> participant)
        {
            Remove<TRequest, TResponse>(topic, participant.Id);
        }

        private class Channel<TRequest, TResponse>
        {
            private readonly ConcurrentDictionary<Guid, IParticipant<TRequest, TResponse>> _participants;

            public Channel()
            {
                _participants = new ConcurrentDictionary<Guid, IParticipant<TRequest, TResponse>>();
            }

            public void Add(IParticipant<TRequest, TResponse> participant)
            {
                _participants.TryAdd(participant.Id, participant);
            }

            public IEnumerable<IParticipant<TRequest, TResponse>> GetAll()
            {
                return _participants.Values.ToArray();
            }

            public void Remove(Guid id)
            {
                _participants.TryRemove(id, out IParticipant<TRequest, TResponse> participant);
            }

            public bool IsEmpty => _participants.IsEmpty;
        }

        private class Token<TRequest, TResponse> : IDisposable
        {
            private readonly SimpleParticipantStore _store;
            private readonly string _topic;
            private readonly Guid _id;

            public Token(SimpleParticipantStore store, string topic, Guid id)
            {
                _store = store;
                _topic = topic;
                _id = id;
            }

            public void Dispose()
            {
                _store.Remove<TRequest, TResponse>(_topic, _id);
            }
        }

        private static string GetKey(Type requestType, Type responseType, string name)
        {
            return $"Request={requestType.AssemblyQualifiedName}:Response={responseType.AssemblyQualifiedName}:Name={name ?? string.Empty}";
        }

        private void Remove<TRequest, TResponse>(string topic, Guid id)
        {
            var key = GetKey(typeof(TRequest), typeof(TResponse), topic);
            if (!_channels.TryGetValue(key, out object channelObj))
                return;
            var channel = channelObj as Channel<TRequest, TResponse>;
            if (channel == null)
                return;
            channel.Remove(id);
            if (channel.IsEmpty)
                _channels.TryRemove(key, out object whatever);
        }
    }
}