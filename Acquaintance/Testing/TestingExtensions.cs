using System;
using System.Linq;

namespace Acquaintance.Testing
{
    public static class TestingExtensions
    {
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

        public static void VerifyAllExpectations(this IMessageBus messageBus)
        {
            GetTestingModule(messageBus).VerifyAllExpectations();
        }

        private static TestingModule GetTestingModule(IMessageBus messageBus)
        {
            TestingModule module = messageBus.Modules.GetByType<TestingModule>().FirstOrDefault();
            if (module != null)
                return module;
            module = new TestingModule();
            messageBus.Modules.Add(module);
            return module;
        }
    }
}
