using System;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.ScatterGather
{
    public interface IParticipantReference<in TRequest, out TResponse>
    {
        TResponse[] Invoke(TRequest request);
        bool IsAlive { get; }
    }

    public class StrongParticipantReference<TRequest, TResponse> : IParticipantReference<TRequest, TResponse>
    {
        private readonly Func<TRequest, IEnumerable<TResponse>> _func;

        public StrongParticipantReference(Func<TRequest, IEnumerable<TResponse>> func)
        {
            _func = func;
        }

        public TResponse[] Invoke(TRequest request)
        {
            var responses = _func(request) ?? Enumerable.Empty<TResponse>();
            return responses.ToArray();
        }

        public bool IsAlive => true;
    }

    public class WeakParticipantReference<TRequest, TResponse> : IParticipantReference<TRequest, TResponse>
    {
        private readonly WeakReference<Func<TRequest, IEnumerable<TResponse>>> _func;

        public WeakParticipantReference(Func<TRequest, IEnumerable<TResponse>> func)
        {
            _func = new WeakReference<Func<TRequest, IEnumerable<TResponse>>>(func);
        }

        public TResponse[] Invoke(TRequest request)
        {
            Func<TRequest, IEnumerable<TResponse>> func;
            if (_func.TryGetTarget(out func))
            {
                var response = func(request) ?? Enumerable.Empty<TResponse>();
                return response.ToArray();
            }

            IsAlive = false;
            return new TResponse[0];
        }

        public bool IsAlive { get; private set; }
    }
}
