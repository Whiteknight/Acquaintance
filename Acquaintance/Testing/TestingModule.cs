using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Acquaintance.Testing
{
    public class TestingModule : IMessageBusModule
    {
        private readonly ConcurrentBag<IExpectation> _expectations;
        private readonly SubscriptionCollection _subscriptions;

        public TestingModule(IMessageBus messageBus)
        {
            _expectations = new ConcurrentBag<IExpectation>();
            _subscriptions = new SubscriptionCollection(messageBus);
        }

        public void VerifyAllExpectations(Action<string[]> onError)
        {
            var messages = _expectations
                .Where(expectation => !expectation.IsMet)
                .Select(expectation => expectation.ToString())
                .ToArray();
            if (messages.Any())
                (onError ?? DefaultOnError)(messages);
        }

        public PublishExpectation<TPayload> ExpectPublish<TPayload>(string topic, Func<TPayload, bool> filter, string description)
        {
            var expectation = new PublishExpectation<TPayload>(topic, description, filter);
            _expectations.Add(expectation);
            _subscriptions.Subscribe<TPayload>(builder => builder
                .WithTopic(topic)
                .Invoke(p => expectation.TryReceive(p))
                .Immediate()
                .WithFilter(filter));
            return expectation;
        }

        public RequestExpectation<TRequest, TResponse> ExpectRequest<TRequest, TResponse>(string topic, Func<TRequest, bool> filter, string description)
        {
            var expectation = new RequestExpectation<TRequest, TResponse>(topic, description, filter);
            _expectations.Add(expectation);
            _subscriptions.Listen<TRequest, TResponse>(l => l
                .WithTopic(topic)
                .Invoke(r => expectation.TryHandle(r))
                .Immediate()
                .WithFilter(filter));
            return expectation;
        }

        public ScatterExpectation<TRequest, TResponse> ExpectScatter<TRequest, TResponse>(string topic, Func<TRequest, bool> filter, string description)
        {
            var expectation = new ScatterExpectation<TRequest, TResponse>(topic, description, filter);
            _expectations.Add(expectation);
            _subscriptions.Participate<TRequest, TResponse>(p => p
                .WithTopic(topic)
                .Invoke(r => expectation.TryHandle(r))
                .Immediate()
                .WithFilter(filter));
            return expectation;
        }

        public void Dispose()
        {
        }

        public void Start()
        {
        }

        public void Stop()
        {
            _subscriptions.Dispose();
        }

        private static void DefaultOnError(string[] errors)
        {
            throw new ExpectationFailedException(errors);
        }
    }
}