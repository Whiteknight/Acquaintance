using System;
using Acquaintance.Utility;

namespace Acquaintance.Sagas
{
    public static class SagaExtensions
    {
        public static IDisposable InitializeSagas(this IMessageBus messageBus, int numberOfThreads = 1)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            var existing = messageBus.Modules.Get<SagasModule>();
            if (existing != null)
                throw new Exception("Sagas module is already initialized");
            return messageBus.Modules.Add(new SagasModule(messageBus, numberOfThreads));
        }

        public static IDisposable CreateSaga<TState, TKey>(this IMessageBus messageBus, Action<ISagaBuilder<TState, TKey>> build)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(build, nameof(build));

            var module = messageBus.Modules.Get<SagasModule>();
            if (module == null)
                throw new Exception($"Must initialize the Sagas module first. Call .{nameof(InitializeSagas)}()");
            var builder = new SagaBuilder<TState, TKey>();
            build(builder);
            var saga = module.CreateSaga<TState, TKey>();
            builder.BuildTo(saga);
            return module.AddSaga(saga);
        }
    }
}