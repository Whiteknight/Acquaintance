using Acquaintance.Threading;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Acquaintance.Sources
{
    public class EventSourceModule : IMessageBusModule, IThreadManager
    {
        private IMessageBus _messageBus;
        private readonly ConcurrentDictionary<Guid, IEventSourceWorker> _threads;

        public EventSourceModule()
        {
            _threads = new ConcurrentDictionary<Guid, IEventSourceWorker>();
        }

        public void Attach(IMessageBus messageBus)
        {
            _messageBus = messageBus;
        }

        public void Start()
        {
        }

        public void Stop()
        {
            foreach (var thread in _threads.Values.ToList())
                thread.Dispose();
            _threads.Clear();
        }

        public void Unattach()
        {
            _messageBus = null;
        }

        public IDisposable RunEventSource(IEventSource source)
        {
            IEventSourceContext context = new EventSourceContext(_messageBus);
            var thread = new EventSourceWorker(source, context);
            bool ok = _threads.TryAdd(thread.Id, thread);
            if (!ok)
            {
                // TODO: Handle the rare error
                return null;
            }
            _messageBus.WorkerPool.RegisterManagedThread(this, thread.ThreadId, "SourceModule thread " + thread.Id);
            return new ThreadToken(this, thread, thread.Id);
        }

        private void RemoveThread(Guid id)
        {
            _threads.TryRemove(id, out IEventSourceWorker thread);
            _messageBus.WorkerPool.UnregisterManagedThread(thread.ThreadId);
        }

        private class ThreadToken : IDisposable
        {
            private readonly IEventSourceWorker _worker;
            private readonly Guid _id;
            private readonly EventSourceModule _module;

            public ThreadToken(EventSourceModule module, IEventSourceWorker worker, Guid id)
            {
                _worker = worker;
                _id = id;
                _module = module;
            }

            public void Dispose()
            {
                _module.RemoveThread(_id);
                _worker.Dispose();
            }
        }

        public void Dispose()
        {
            // TODO: This
        }
    }
}
