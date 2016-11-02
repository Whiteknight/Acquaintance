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
            switch (options.DispatchType)
            {
                case DispatchThreadType.AnyWorkerThread:
                    return new AnyThreadListener<TRequest, TResponse>(func, filter, _threadPool, options.WaitTimeoutMs);
                case DispatchThreadType.SpecificThread:
                    return new SpecificThreadListener<TRequest, TResponse>(func, filter, options.ThreadId, _threadPool, options.WaitTimeoutMs);
                default:
                    return new ImmediateListener<TRequest, TResponse>(func, filter);
            }
        }
    }
}