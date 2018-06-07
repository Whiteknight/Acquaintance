using System;
using Acquaintance.Logging;
using Acquaintance.Modules;
using Acquaintance.Threading;
using Acquaintance.Utility;

namespace Acquaintance.Sources
{
    public class EventSourceContext : IntervalWorkerContext, IEventSourceContext
    {
        private readonly IPubSubBus _messageBus;

        public EventSourceContext(IPubSubBus messageBus)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            _messageBus = messageBus;
            IterationDelayMs = -1;
            Modules = new ReadOnlyModules(messageBus);
            WorkerPool = new ReadOnlyWorkerPool(messageBus.WorkerPool);
        }

        public string Id => _messageBus.Id;
        public ILogger Logger => _messageBus.Logger;

        public IEnvelopeFactory EnvelopeFactory => _messageBus.EnvelopeFactory;

        public IModuleManager Modules { get; }

        public IWorkerPool WorkerPool { get; }

        public void PublishEnvelope<TPayload>(Envelope<TPayload> envelope)
        {
            _messageBus.PublishEnvelope(envelope);
        }

        public class ReadOnlyModules : IModuleManager
        {
            private readonly IBusBase _messageBus;

            public ReadOnlyModules(IBusBase messageBus)
            {
                _messageBus = messageBus;
            }

            public IDisposable Add<TModule>(TModule module)
                where TModule : class, IMessageBusModule
            {
                throw new Exception("Cannot add a new module in this context");
            }

            public TModule Get<TModule>()
                where TModule : class, IMessageBusModule
            {
                return _messageBus.Modules.Get<TModule>();
            }
        }

        public class ReadOnlyWorkerPool : IWorkerPool
        {
            private readonly IWorkerPool _workerPool;

            public ReadOnlyWorkerPool(IWorkerPool workerPool)
            {
                _workerPool = workerPool;
            }

            public int NumberOfRunningFreeWorkers => _workerPool.NumberOfRunningFreeWorkers;

            public ThreadReport GetThreadReport()
            {
                return _workerPool.GetThreadReport();
            }

            private void ThrowReadOnlyException()
            {
                throw new Exception("Cannot modify the worker pool in this context");
            }

            public WorkerToken StartDedicatedWorker()
            {
                ThrowReadOnlyException();
                return null;
            }

            public void StopDedicatedWorker(int threadId)
            {
                ThrowReadOnlyException();
            }

            public IActionDispatcher GetDispatcher(int threadId, bool allowAutoCreate)
            {
                return _workerPool.GetDispatcher(threadId, allowAutoCreate);
            }

            public IActionDispatcher GetFreeWorkerDispatcher()
            {
                return _workerPool.GetFreeWorkerDispatcher();
            }

            public IActionDispatcher GetThreadPoolDispatcher()
            {
                return _workerPool.GetThreadPoolDispatcher();
            }

            public IActionDispatcher GetAnyWorkerDispatcher()
            {
                return _workerPool.GetAnyWorkerDispatcher();
            }

            public IActionDispatcher GetCurrentThreadDispatcher()
            {
                return _workerPool.GetCurrentThreadDispatcher();
            }

            public IWorkerContext GetCurrentThreadContext()
            {
                return _workerPool.GetCurrentThreadContext();
            }

            public IDisposable RegisterManagedThread(string owner, int threadId, string purpose)
            {
                ThrowReadOnlyException();
                return null;
            }
        }
    }
}
