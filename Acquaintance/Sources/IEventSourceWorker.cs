using System;
using Acquaintance.Threading;

namespace Acquaintance.Sources
{
    public class EventSourceWorker : IDisposable
    {
        private readonly IntervalWorkerThread _thread;
        private readonly IEventSourceContext _context;

        public EventSourceWorker(IntervalWorkerThread thread, IEventSourceContext context)
        {
            _thread = thread;
            _context = context;
        }

        public Guid Id => _thread.Id;
        public int ThreadId => _thread.ThreadId;


        public void Dispose()
        {
            _thread?.Dispose();
        }
    }
}