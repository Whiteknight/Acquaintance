using System;

namespace Acquaintance.RequestResponse
{
    /// <summary>
    /// A reference to a listener callback or other handler. The reference may optionally be a weak
    /// reference so the target might be garbage collected at any time.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public interface IListenerReference<in TRequest, out TResponse>
    {
        TResponse Invoke(TRequest request);
        bool IsAlive { get; }
    }

    public class StrongListenerReference<TRequest, TResponse> : IListenerReference<TRequest, TResponse>
    {
        private readonly Func<TRequest, TResponse> _func;

        public StrongListenerReference(Func<TRequest, TResponse> func)
        {
            _func = func;
        }

        public TResponse Invoke(TRequest request)
        {
            return _func(request);
        }

        public bool IsAlive => true;
    }

    public class WeakListenerReference<TRequest, TResponse> : IListenerReference<TRequest, TResponse>
    {
        private readonly WeakReference<Func<TRequest, TResponse>> _func;

        public WeakListenerReference(Func<TRequest, TResponse> func)
        {
            _func = new WeakReference<Func<TRequest, TResponse>>(func);
            IsAlive = true;
        }

        public TResponse Invoke(TRequest request)
        {
            Func<TRequest, TResponse> func;
            if (_func.TryGetTarget(out func))
                return func(request);

            IsAlive = false;
            return default(TResponse);
        }

        public bool IsAlive { get; private set; }
    }
}
