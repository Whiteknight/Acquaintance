using Acquaintance.Threading;
using System;
using System.Collections.Concurrent;
using System.Linq;
using Acquaintance.Logging;
using Acquaintance.Utility;

namespace Acquaintance.Sources
{
    public class EventSourceModule : IMessageBusModule
    {
        private readonly IPubSubBus _messageBus;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<Guid, IntervalWorkerThread> _threads;
        private readonly ConcurrentDictionary<Guid, IDisposable> _tokens;

        public EventSourceModule(IPubSubBus messageBus, ILogger logger)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));

            _messageBus = messageBus;
            _logger = logger ?? messageBus.Logger;
            _threads = new ConcurrentDictionary<Guid, IntervalWorkerThread>();
            _tokens = new ConcurrentDictionary<Guid, IDisposable>();   
        }

        public void Start()
        {
        }

        public void Stop()
        {
            foreach (var token in _tokens.Values.ToList())
                token.Dispose();
            _tokens.Clear();
        }

        public IDisposable RunEventSource(IEventSource source)
        {
            Assert.ArgumentNotNull(source, nameof(source));

            IEventSourceContext context = new EventSourceContext(_messageBus);
            var strategy = new EventSourceWorkStrategy(source, context);
            var thread = new IntervalWorkerThread(_messageBus.Logger, strategy);

            if (!_threads.TryAdd(thread.Id, thread))
            {
                _logger.Error($"Could not add new event source ThreadId={thread.Id}. Maybe it has already been added?");
                thread.Dispose();
                return null;
            }

            var threadToken = _messageBus.WorkerPool.RegisterManagedThread("Event Source Module", thread.ThreadId, "SourceModule thread " + thread.Id);
            var workerToken = new WorkerToken(this, thread, thread.Id, threadToken);
            _tokens.TryAdd(thread.Id, workerToken);
            thread.Start();
            return workerToken;
        }

        private void RemoveThread(Guid id)
        {
            _tokens.TryRemove(id);
            _threads.TryRemoveAndDispose(id);
        }

        private class WorkerToken : DisposeOnceToken
        {
            private readonly IntervalWorkerThread _worker;
            private readonly Guid _id;
            private readonly IDisposable _threadToken;
            private readonly EventSourceModule _module;

            public WorkerToken(EventSourceModule module, IntervalWorkerThread worker, Guid id, IDisposable threadToken)
            {
                _worker = worker;
                _id = id;
                _threadToken = threadToken;
                _module = module;
            }

            protected override void Dispose(bool disposing)
            {
                _threadToken.Dispose();
                _module.RemoveThread(_id);
            }

            public override string ToString()
            {
                return $"EventSourceWorker Id={_id} ThreadId={_worker.ThreadId}";
            }
        }

        public void Dispose()
        {
            foreach (var token in _tokens.Values)
            {
                try
                {
                    token.Dispose();
                }
                catch { }
            }
            _tokens.Clear();
            

            foreach (var source in _threads.Values)
            {
                try
                {
                    source.Dispose();
                }
                catch { }
            }
            _threads.Clear();
        }
    }
}
