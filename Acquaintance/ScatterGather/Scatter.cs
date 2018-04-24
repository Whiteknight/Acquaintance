using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Acquaintance.ScatterGather
{
    public class Scatter<TResponse> : IScatter<TResponse>, IGatherReceiver<TResponse>
    {
        private const int DefaultTimeoutS = 10;
        private readonly BlockingCollection<ScatterResponse<TResponse>> _responses;

        private volatile bool _neverHadParticipants;
        private volatile int _expectCount;
        private volatile int _totalParticipants;
        private volatile int _completedParticipants;

        public Scatter()
        {
            _responses = new BlockingCollection<ScatterResponse<TResponse>>();
            _neverHadParticipants = true;
        }

        public int TotalParticipants => _totalParticipants;

        public int CompletedParticipants => _completedParticipants;

        public ScatterResponse<TResponse> GetNextResponse(TimeSpan timeout)
        {
            if (_neverHadParticipants)
                return null;

            if (_expectCount == 0 && _responses.Count == 0)
                return null;

            var ok = _responses.TryTake(out ScatterResponse<TResponse> response, timeout);
            return ok ? response : null;
        }

        public async Task<TResponse> GetNextResponseAsync(int timeoutMs, CancellationToken token)
        {
            if (_neverHadParticipants)
                return default(TResponse);

            if (_expectCount == 0 && _responses.Count == 0)
                return default(TResponse);

            return await Task
                .Run(() =>
                {
                    var ok = _responses.TryTake(out ScatterResponse<TResponse> response, timeoutMs, token);
                    if (!ok)
                        return default(TResponse);

                    response.ThrowExceptionIfPresent();
                    return response.Value;
                }, token)
                .ConfigureAwait(false);
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
            if (max > TotalParticipants)
                max = TotalParticipants;

            var endTime = DateTime.UtcNow + timeout;
            var responses = new List<ScatterResponse<TResponse>>();
            while (timeout.TotalMilliseconds > 0)
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

        public void AddResponse(Guid participantId, ScatterResponse<TResponse> response)
        {
            // TODO: Check to make sure we don't add multiple responses from a single participant
            Interlocked.Increment(ref _completedParticipants);
            _responses.Add(response);
            Interlocked.Decrement(ref _expectCount);
        }

        public void AddParticipant(Guid participantId)
        {
            Interlocked.Increment(ref _totalParticipants);
            _neverHadParticipants = false;
            Interlocked.Increment(ref _expectCount);
        }

        public void Dispose()
        {
            _responses.Dispose();
        }
    }
}
