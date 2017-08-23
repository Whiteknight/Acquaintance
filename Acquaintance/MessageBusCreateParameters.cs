using Acquaintance.Logging;
using Acquaintance.Threading;

namespace Acquaintance
{
    public class MessageBusCreateParameters
    {
        public bool AllowWildcards { get; set; }
        public IThreadPool ThreadPool { get; set; }
        public ILogger Logger { get; set; }

        public static MessageBusCreateParameters Default => new MessageBusCreateParameters();

        public MessageBusCreateParameters WithWildcards()
        {
            AllowWildcards = true;
            return this;
        }

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
    }
}
