using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Acquaintance.ScatterGather
{
    public class Scatter<TResponse> : IScatter<TResponse>
    {
        private const int DefaultTimeoutS = 10;
        private readonly BlockingCollection<ScatterResponse<TResponse>> _responses;

        private bool _isComplete;
        private bool _neverHadParticipants;
        private int _expectCount;
        private int _totalParticipants;
        private int _completedParticipants;

        public Scatter()
        {
            _responses = new BlockingCollection<ScatterResponse<TResponse>>();
            _isComplete = false;
            _neverHadParticipants = true;
        }

        public ScatterResponse<TResponse> GetNextResponse(TimeSpan timeout)
        {
            if (_neverHadParticipants)
                return null;

            if (_expectCount == 0 && _responses.Count == 0)
                return null;

            var ok = _responses.TryTake(out ScatterResponse<TResponse> response, timeout);
            if (ok)
                return response;

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
            return GatherResponses(TotalParticipants, timeout);
        }

        public IReadOnlyList<ScatterResponse<TResponse>> GatherResponses()
        {
            return GatherResponses(int.MaxValue, new TimeSpan(0, 0, DefaultTimeoutS));
        }

        public void AddResponse(Guid participantId, TResponse response)
        {
            Interlocked.Increment(ref _completedParticipants);
            _responses.Add(new ScatterResponse<TResponse>(response, participantId, null));
            Interlocked.Decrement(ref _expectCount);
        }

        public void AddError(Guid participantId, Exception error)
        {
            Interlocked.Increment(ref _completedParticipants);
            _responses.Add(new ScatterResponse<TResponse>(default(TResponse), participantId, error));
            Interlocked.Decrement(ref _expectCount);
        }

        public void AddParticipant(Guid participantId)
        {
            Interlocked.Increment(ref _totalParticipants);
            _neverHadParticipants = false;
            Interlocked.Increment(ref _expectCount);
        }

        public int TotalParticipants => _totalParticipants;

        public int CompletedParticipants => _completedParticipants;

        public void Dispose()
        {
            _responses.Dispose();
        }

        private bool CheckCompleteness()
        {
            if (_isComplete)
                return true;
            if (_expectCount == 0)
            {
                _isComplete = true;
                return true;
            }
            return false;
        }
    }
}
