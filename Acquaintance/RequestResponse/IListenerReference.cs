using System;

namespace Acquaintance.RequestResponse
{
    /// <summary>
    /// A reference to a listener callback or other handler. The reference may optionally be a weak
    /// reference so the target might be garbage collected at any time.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public interface IListenerReference<TRequest, out TResponse>
    {
        TResponse Invoke(Envelope<TRequest> request);
        bool IsAlive { get; }
    }

    public class EnvelopeStrongListenerReference<TRequest, TResponse> : IListenerReference<TRequest, TResponse>
    {
        private readonly Func<Envelope<TRequest>, TResponse> _func;

        public EnvelopeStrongListenerReference(Func<Envelope<TRequest>, TResponse> func)
        {
            _func = func;
        }

        public TResponse Invoke(Envelope<TRequest> request)
        {
            return _func(request);
        }

        public bool IsAlive => true;
    }

    public class PayloadStrongListenerReference<TRequest, TResponse> : IListenerReference<TRequest, TResponse>
    {
        private readonly Func<TRequest, TResponse> _func;

        public PayloadStrongListenerReference(Func<TRequest, TResponse> func)
        {
            _func = func;
        }

        public TResponse Invoke(Envelope<TRequest> request)
        {
            return _func(request.Payload);
        }

        public bool IsAlive => true;
    }

    public class EnvelopeWeakListenerReference<TRequest, TResponse> : IListenerReference<TRequest, TResponse>
    {
        private readonly WeakReference<Func<Envelope<TRequest>, TResponse>> _func;

        public EnvelopeWeakListenerReference(Func<Envelope<TRequest>, TResponse> func)
        {
            _func = new WeakReference<Func<Envelope<TRequest>, TResponse>>(func);
            IsAlive = true;
        }

        public TResponse Invoke(Envelope<TRequest> request)
        {
            if (_func.TryGetTarget(out Func<Envelope<TRequest>, TResponse> func))
                return func(request);

            IsAlive = false;
            return default(TResponse);
        }

        public bool IsAlive { get; private set; }
    }

    public class PayloadWeakListenerReference<TRequest, TResponse> : IListenerReference<TRequest, TResponse>
    {
        private readonly WeakReference<Func<TRequest, TResponse>> _func;

        public PayloadWeakListenerReference(Func<TRequest, TResponse> func)
        {
            _func = new WeakReference<Func<TRequest, TResponse>>(func);
            IsAlive = true;
        }

        public TResponse Invoke(Envelope<TRequest> request)
        {
            if (_func.TryGetTarget(out Func<TRequest, TResponse> func))
                return func(request.Payload);

            IsAlive = false;
            return default(TResponse);
        }

        public bool IsAlive { get; private set; }
    }
}
