using Acquaintance.Threading;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Acquaintance.Sources
{
    public class EventSourceModule : IMessageBusModule, IThreadManager
    {
        private IMessageBus _messageBus;
        private readonly ConcurrentDictionary<Guid, IEventSourceThread> _threads;

        public EventSourceModule()
        {
            _threads = new ConcurrentDictionary<Guid, IEventSourceThread>();
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
            {
                thread.Dispose();
            }
            _threads.Clear();
        }

        public void Unattach()
        {
            _messageBus = null;
        }

        public IDisposable RunEventSource(IEventSource source)
        {
            IEventSourceContext context = new EventSourceContext(_messageBus);
            var thread = new EventSourceThread(source, context);
            bool ok = _threads.TryAdd(thread.Id, thread);
            if (!ok)
            {
                // TODO: Handle the rare error
            }
            _messageBus.ThreadPool.RegisterManagedThread(this, thread.ThreadId, "SourceModule thread " + thread.Id);
            return new ThreadToken(this, thread, thread.Id);
        }

        private void RemoveThread(Guid id)
        {
            IEventSourceThread thread;
            _threads.TryRemove(id, out thread);
            _messageBus.ThreadPool.UnregisterManagedThread(thread.ThreadId);
        }

        private class ThreadToken : IDisposable
        {
            private readonly IEventSourceThread _thread;
            private readonly Guid _id;
            private readonly EventSourceModule _module;

            public ThreadToken(EventSourceModule module, IEventSourceThread thread, Guid id)
            {
                _thread = thread;
                _id = id;
                _module = module;
            }

            public void Dispose()
            {
                _module.RemoveThread(_id);
                _thread.Dispose();
            }
        }

        public void Dispose()
        {
            // TODO: This
        }
    }
}
