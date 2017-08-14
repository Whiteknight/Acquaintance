using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.ScatterGather
{
    public class ScatterResponse<TResponse>
    {
        public ScatterResponse(TResponse response, Guid participantId, Exception errorInformation)
        {
            Response = response;
            ParticipantId = participantId;
            ErrorInformation = errorInformation;
            Success = errorInformation == null;
        }

        public TResponse Response { get; private set; }
        public Guid ParticipantId { get; }
        public bool Success { get; private set; }
        public Exception ErrorInformation { get; }
        public bool Completed { get; set; }

        public void ThrowExceptionIfPresent()
        {
            if (ErrorInformation != null)
                throw ErrorInformation;
        }
    }

    public interface IScatter<TResponse> : IDisposable
    {
        ScatterResponse<TResponse> GetNextResponse(TimeSpan timeout);
        ScatterResponse<TResponse> GetNextResponse(int timeoutMs);
        ScatterResponse<TResponse> GetNextResponse();
        IReadOnlyList<ScatterResponse<TResponse>> GatherResponses(int max, TimeSpan timeout);
        IReadOnlyList<ScatterResponse<TResponse>> GatherResponses(int max);
        IReadOnlyList<ScatterResponse<TResponse>> GatherResponses(TimeSpan timeout);
        IReadOnlyList<ScatterResponse<TResponse>> GatherResponses();
        bool IsComplete();
    }

    public class Scatter<TResponse> : IScatter<TResponse>
    {
        private const int DefaultTimeoutS = 10;
        private readonly BlockingCollection<ScatterResponse<TResponse>> _responses;
        private readonly ConcurrentDictionary<Guid, bool> _activeParticipants;
        private bool _isComplete;
        private bool _neverHadParticipants;

        public Scatter()
        {
            _activeParticipants = new ConcurrentDictionary<Guid, bool>();
            _responses = new BlockingCollection<ScatterResponse<TResponse>>();
            _isComplete = false;
            _neverHadParticipants = true;
        }

        public ScatterResponse<TResponse> GetNextResponse(TimeSpan timeout)
        {
            if (_neverHadParticipants)
                return null;

            if (_activeParticipants.Count == 0 && _responses.Count == 0)
                return null;

            var ok = _responses.TryTake(out ScatterResponse<TResponse> response, timeout);
            if (ok)
                return response;

            CheckCompleteness();
            return null;
        }

        public ScatterResponse<TResponse> GetNextResponse(int timeoutMs)
        {
            return GetNextResponse(TimeSpan.FromMilliseconds(timeoutMs));
        }

        public ScatterResponse<TResponse> GetNextResponse()
        {
            return GetNextResponse(new TimeSpan(0, 0, DefaultTimeoutS));
        }

        public IReadOnlyList<ScatterResponse<TResponse>> GatherResponses(int max, TimeSpan timeout)
        {
            var endTime = DateTime.UtcNow + timeout;
            var responses = new List<ScatterResponse<TResponse>>();
            while(timeout.TotalMilliseconds > 0)
            {
                var response = GetNextResponse(timeout);
                if (response == null)
                    break;
                
                responses.Add(response);
                if (responses.Count >= max)
                    break;
                
                var now = DateTime.UtcNow;
                if (now >= endTime)
                    break;
                timeout = endTime - now;
            }
            return responses;
        }

        public IReadOnlyList<ScatterResponse<TResponse>> GatherResponses(int max)
        {
            return GatherResponses(max, new TimeSpan(0, 0, DefaultTimeoutS));
        }

        public IReadOnlyList<ScatterResponse<TResponse>> GatherResponses(TimeSpan timeout)
        {
            return GatherResponses(int.MaxValue, timeout);
        }

        public IReadOnlyList<ScatterResponse<TResponse>> GatherResponses()
        {
            return GatherResponses(int.MaxValue, new TimeSpan(0, 0, DefaultTimeoutS));
        }

        public void AddResponse(Guid participantId, TResponse response)
        {
            if (!_activeParticipants.ContainsKey(participantId))
                return;
            _responses.Add(new ScatterResponse<TResponse>(response, participantId, null));
            _activeParticipants.TryRemove(participantId, out bool whatever);
        }

        public void AddError(Guid participantId, Exception error)
        {
            if (!_activeParticipants.ContainsKey(participantId))
                return;
            _responses.Add(new ScatterResponse<TResponse>(default(TResponse), participantId, error));
            _activeParticipants.TryRemove(participantId, out bool whatever);
        }

        public void AddParticipant(Guid participantId)
        {
            _neverHadParticipants = false;
            _activeParticipants.TryAdd(participantId, true);
        }

        public bool IsComplete()
        {
            return CheckCompleteness();
        }

        public void Dispose()
        {
            _responses.Dispose();
        }

        private bool CheckCompleteness()
        {
            if (_isComplete)
                return true;
            bool hasMore = _activeParticipants.Any();
            if (hasMore)
                return false;
            _isComplete = true;
            return true;
        }
    }
}
