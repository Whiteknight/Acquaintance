using Acquaintance.Threading;
using System;

namespace Acquaintance.RequestResponse
{
    public class ListenerFactory
    {
        private readonly MessagingWorkerThreadPool _threadPool;

        public ListenerFactory(MessagingWorkerThreadPool threadPool)
        {
            _threadPool = threadPool;
        }

        public IListener<TRequest, TResponse> CreateListener<TRequest, TResponse>(Func<TRequest, TResponse> func, Func<TRequest, bool> filter, ListenOptions options)
        {
            options = options ?? ListenOptions.Default;
            var reference = CreateReference(func, options.KeepAlive);

            IListener<TRequest, TResponse> listener;
            switch (options.DispatchType)
            {
                case DispatchThreadType.AnyWorkerThread:
                    listener = new AnyThreadListener<TRequest, TResponse>(reference, _threadPool, options.WaitTimeoutMs);
                    break;
                case DispatchThreadType.SpecificThread:
                    listener = new SpecificThreadListener<TRequest, TResponse>(reference, options.ThreadId, _threadPool, options.WaitTimeoutMs);
                    break;
                default:
                    listener = new ImmediateListener<TRequest, TResponse>(reference);
                    break;
            }
            if (filter != null)
                listener = new FilteredListener<TRequest, TResponse>(listener, filter);
            return listener;
        }

        private IListenerReference<TRequest, TResponse> CreateReference<TRequest, TResponse>(Func<TRequest, TResponse> listener, bool keepAlive)
        {
            if (keepAlive)
                return new StrongListenerReference<TRequest, TResponse>(listener);
            return new WeakListenerReference<TRequest, TResponse>(listener);
        }
    }
}