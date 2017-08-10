using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Acquaintance.Testing
{
    public class TestingModule : IMessageBusModule
    {
        private readonly ConcurrentBag<IExpectation> _expectations;
        private SubscriptionCollection _subscriptions;

        public TestingModule()
        {
            _expectations = new ConcurrentBag<IExpectation>();
        }

        public void VerifyAllExpectations()
        {
            var _messages = _expectations
                .Where(expectation => !expectation.IsMet)
                .Select(expectation => expectation.ToString())
                .ToList();
            if (_messages.Any())
                throw new ExpectationFailedException(_messages);
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

        public void Attach(IMessageBus messageBus)
        {
            _subscriptions = new SubscriptionCollection(messageBus);
        }

        public void Unattach()
        {
            _subscriptions.Dispose();
            _subscriptions = null;
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }
    }
}