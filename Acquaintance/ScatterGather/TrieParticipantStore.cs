using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Acquaintance.Utility;

namespace Acquaintance.ScatterGather
{
    public class TrieParticipantStore : IParticipantStore
    {
        private readonly StringTrie<object> _channels;

        public TrieParticipantStore()
        {
            _channels = new StringTrie<object>();
        }

        public IDisposable Participate<TRequest, TResponse>(string topic, IParticipant<TRequest, TResponse> participant)
        {
            var channelObj = _channels.GetOrInsert(typeof(TRequest).FullName, typeof(TResponse).FullName, topic.Split('.'), () => new Channel<TRequest, TResponse>());
            var channel = channelObj as Channel<TRequest, TResponse>;
            if (channel == null)
                throw new Exception("Could not get channel");
            participant.Id = Guid.NewGuid();
            channel.Add(participant);
            return new Token<TRequest, TResponse>(this, typeof(TRequest).Name, typeof(TResponse).Name, topic, participant.Id);
        }

        public IEnumerable<IParticipant<TRequest, TResponse>> GetParticipants<TRequest, TResponse>(string topic)
        {
            return _channels.Get(typeof(TRequest).FullName, typeof(TResponse).FullName, topic.Split('.'))
                .OfType<Channel<TRequest, TResponse>>()
                .SelectMany(c => c.GetAll())
                .ToArray();
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
            private readonly TrieParticipantStore _store;
            private readonly string _requestType;
            private readonly string _responseType;
            private readonly string _topic;
            private readonly Guid _id;

            public Token(TrieParticipantStore store, string requestType, string responseType, string topic, Guid id)
            {
                _store = store;
                _requestType = requestType;
                _responseType = responseType;
                _topic = topic;
                _id = id;
            }

            public void Dispose()
            {
                _store.Remove<TRequest, TResponse>(_topic, _id);
            }

            public override string ToString()
            {
                return $"Participant Request={_requestType} Response={_responseType} Topic={_topic} Id={_id}";
            }
        }

        private void Remove<TRequest, TResponse>(string topic, Guid id)
        {
            var root1 = typeof(TRequest).FullName;
            var root2 = typeof(TResponse).FullName;
            var path = topic.Split('.');
            var channelObj = _channels.Get(root1, root2, path).FirstOrDefault();
            var channel = channelObj as Channel<TRequest, TResponse>;
            if (channel == null)
                return;
            channel.Remove(id);
            if (channel.IsEmpty)
                _channels.RemoveValue(root1, root2, path, v => (v as IDisposable)?.Dispose());
        }
    }
}

