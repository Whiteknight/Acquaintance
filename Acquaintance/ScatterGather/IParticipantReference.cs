using System;

namespace Acquaintance.ScatterGather
{
    public interface IParticipantReference<TRequest, out TResponse>
    {
        TResponse Invoke(Envelope<TRequest> request);
        bool IsAlive { get; }
    }

    public class StrongParticipantReference<TRequest, TResponse> : IParticipantReference<TRequest, TResponse>
    {
        private readonly Func<TRequest, TResponse> _func;

        public StrongParticipantReference(Func<TRequest, TResponse> func)
        {
            _func = func;
        }

        public bool IsAlive => true;

        public TResponse Invoke(Envelope<TRequest> request)
        {
            return _func(request.Payload);
        }
    }

    public class WeakParticipantReference<TRequest, TResponse> : IParticipantReference<TRequest, TResponse>
    {
        private readonly WeakReference<Func<TRequest, TResponse>> _func;

        public WeakParticipantReference(Func<TRequest, TResponse> func)
        {
            _func = new WeakReference<Func<TRequest, TResponse>>(func);
            IsAlive = true;
        }

        public bool IsAlive { get; private set; }

        public TResponse Invoke(Envelope<TRequest> request)
        {
            if (_func.TryGetTarget(out Func<TRequest, TResponse> func))
                return func(request.Payload);

            IsAlive = false;
            return default(TResponse);
        }
    }

    public class EnvelopeStrongParticipantReference<TRequest, TResponse> : IParticipantReference<TRequest, TResponse>
    {
        private readonly Func<Envelope<TRequest>, TResponse> _func;

        public EnvelopeStrongParticipantReference(Func<Envelope<TRequest>, TResponse> func)
        {
            _func = func;
        }

        public bool IsAlive => true;

        public TResponse Invoke(Envelope<TRequest> request)
        {
            return _func(request);
        }
    }

    public class EnvelopeWeakParticipantReference<TRequest, TResponse> : IParticipantReference<TRequest, TResponse>
    {
        private readonly WeakReference<Func<Envelope<TRequest>, TResponse>> _func;

        public EnvelopeWeakParticipantReference(Func<Envelope<TRequest>, TResponse> func)
        {
            _func = new WeakReference<Func<Envelope<TRequest>, TResponse>>(func);
            IsAlive = true;
        }

        public bool IsAlive { get; private set; }

        public TResponse Invoke(Envelope<TRequest> request)
        {
            if (_func.TryGetTarget(out Func<Envelope<TRequest>, TResponse> func))
                return func(request);

            IsAlive = false;
            return default(TResponse);
        }
    }
}
