using Acquaintance.Logging;
using Acquaintance.Threading;

namespace Acquaintance
{
    public class MessageBusCreateParameters
    {
        public IThreadPool ThreadPool { get; set; }
        public ILogger Logger { get; set; }
        public IDispatchStrategyFactory DispatchStrategy { get; set; }

        public static MessageBusCreateParameters Default => new MessageBusCreateParameters();

        public ILogger GetLogger()
        {
            if (Logger != null)
                return Logger;
#if DEBUG
            return new DelegateLogger(s => System.Diagnostics.Debug.WriteLine(s));
#else
            return new SilentLogger();
#endif
        }

        public IThreadPool GetThreadPool(ILogger log)
        {
            return ThreadPool ?? new MessagingWorkerThreadPool(log, 2);
        }

        public IDispatchStrategyFactory GetDispatchStrategyFactory()
        {
            return DispatchStrategy ?? new SimpleDispatchStrategyFactory();
        }
    }
}
