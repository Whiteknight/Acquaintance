using System;
using System.Linq;
using Acquaintance.Utility;

namespace Acquaintance.Testing
{
    public static class TestingExtensions
    {
        public static PublishExpectation<TPayload> ExpectPublish<TPayload>(this IMessageBus messageBus, string topic, Func<TPayload, bool> filter = null, string description = null)
        {
            return GetTestingModule(messageBus).ExpectPublish(topic, filter, description);
        }

        public static RequestExpectation<TRequest, TResponse> ExpectRequest<TRequest, TResponse>(this IMessageBus messageBus, string topic, Func<TRequest, bool> filter = null, string description = null)
        {
            return GetTestingModule(messageBus).ExpectRequest<TRequest, TResponse>(topic, filter, description);
        }

        public static ScatterExpectation<TRequest, TResponse> ExpectScatter<TRequest, TResponse>(this IMessageBus messageBus, string topic, Func<TRequest, bool> filter = null, string description = null)
        {
            return GetTestingModule(messageBus).ExpectScatter<TRequest, TResponse>(topic, filter, description);
        }

        public static void VerifyAllExpectations(this IMessageBus messageBus, Action<string[]> onError = null)
        {
            GetTestingModule(messageBus).VerifyAllExpectations(onError);
        }

        public static IDisposable InitializeTesting(this IMessageBus messageBus)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
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
