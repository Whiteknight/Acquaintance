using System.Threading;
using Acquaintance.Threading;
using Acquaintance.Utility;

namespace Acquaintance.Sources
{
    public class EventSourceWorkStrategy : IIntervalWorkStrategy
    {
        private readonly IEventSource _source;
        private readonly IEventSourceContext _context;

        public EventSourceWorkStrategy(IEventSource source, IEventSourceContext context)
        {
            Assert.ArgumentNotNull(source, nameof(source));
            Assert.ArgumentNotNull(context, nameof(context));

            _source = source;
            _context = context;
        }

        public IIntervalWorkerContext CreateContext()
        {
            return _context;
        }

        public void DoWork(IIntervalWorkerContext context, CancellationTokenSource tokenSource)
        {
            _source.CheckForEvents(_context, tokenSource.Token);
        }
    }
}