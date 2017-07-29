using System;
using System.Linq;
using Acquaintance.Utility;

namespace Acquaintance.Testing
{
    public static class TestingExtensions
    {
        private static readonly Guid _id = Guid.NewGuid();

        public static PublishExpectation<TPayload> ExpectPublish<TPayload>(this IMessageBus messageBus, string name, Func<TPayload, bool> filter = null, string description = null)
        {
            return GetTestingModule(messageBus).ExpectPublish(name, filter, description);
        }

        public static RequestExpectation<TRequest, TResponse> ExpectRequest<TRequest, TResponse>(this IMessageBus messageBus, string name, Func<TRequest, bool> filter = null, string description = null)
        {
            return GetTestingModule(messageBus).ExpectRequest<TRequest, TResponse>(name, filter, description);
        }

        public static ScatterExpectation<TRequest, TResponse> ExpectScatter<TRequest, TResponse>(this IMessageBus messageBus, string name, Func<TRequest, bool> filter = null, string description = null)
        {
            return GetTestingModule(messageBus).ExpectScatter<TRequest, TResponse>(name, filter, description);
        }

        public static void VerifyAllExpectations(this IMessageBus messageBus, Action<string[]> onError = null)
        {
            GetTestingModule(messageBus).VerifyAllExpectations(onError);
        }

        public static IDisposable InitializeTesting(this IMessageBus messageBus)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            var module = messageBus.Modules.Get<TestingModule>().FirstOrDefault();
            if (module != null)
                throw new Exception("Testing is already initialized");
            return messageBus.Modules.Add(new TestingModule());
        }

        private static TestingModule GetTestingModule(IMessageBus messageBus)
        {
            var module = messageBus.Modules.Get<TestingModule>().FirstOrDefault();
            if (module == null)
                throw new Exception($"Testing module is not initialized. Call .{nameof(InitializeTesting)}() first");
            return module;
        }
    }
}
