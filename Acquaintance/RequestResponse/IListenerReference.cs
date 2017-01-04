using System;

namespace Acquaintance.RequestResponse
{
    public interface IListenerReference<in TRequest, out TResponse>
    {
        TResponse[] Invoke(TRequest request);
        bool IsAlive { get; }
    }

    public class StrongListenerReference<TRequest, TResponse> : IListenerReference<TRequest, TResponse>
    {
        private readonly Func<TRequest, TResponse> _func;

        public StrongListenerReference(Func<TRequest, TResponse> func)
        {
            _func = func;
        }

        public TResponse[] Invoke(TRequest request)
        {
            var response = _func(request);
            return new[] { response };
        }

        public bool IsAlive => true;
    }

    public class WeakListenerReference<TRequest, TResponse> : IListenerReference<TRequest, TResponse>
    {
        private readonly WeakReference<Func<TRequest, TResponse>> _func;

        public WeakListenerReference(Func<TRequest, TResponse> func)
        {
            _func = new WeakReference<Func<TRequest, TResponse>>(func);
        }

        public TResponse[] Invoke(TRequest request)
        {
            Func<TRequest, TResponse> func;
            if (_func.TryGetTarget(out func))
            {
                var response = func(request);
                return new[] { response };
            }

            IsAlive = false;
            return new TResponse[0];
        }

        public bool IsAlive { get; private set; }
    }
}
