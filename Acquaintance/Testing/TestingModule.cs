﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            List<string> _messages = _expectations.Where(expectation => !expectation.IsMet).Select(expectation => expectation.ToString()).ToList();
            if (_messages.Any())
                throw new ExpectationFailedException(_messages);
        }

        public PublishExpectation<TPayload> ExpectPublish<TPayload>(string name, Func<TPayload, bool> filter, string description)
        {
            var expectation = new PublishExpectation<TPayload>(name, description, filter);
            _expectations.Add(expectation);
            _subscriptions.Subscribe(name, p => expectation.TryReceive(p), filter);
            return expectation;
        }

        public RequestExpectation<TRequest, TResponse> ExpectRequest<TRequest, TResponse>(string name, Func<TRequest, bool> filter, string description)
        {
            var expectation = new RequestExpectation<TRequest, TResponse>(name, description, filter);
            _expectations.Add(expectation);
            _subscriptions.Listen(name, r => expectation.TryHandle(r), filter);
            return expectation;
        }

        public ScatterExpectation<TRequest, TResponse> ExpectScatter<TRequest, TResponse>(string name, Func<TRequest, bool> filter, string description)
        {
            var expectation = new ScatterExpectation<TRequest, TResponse>(name, description, filter);
            _expectations.Add(expectation);
            _subscriptions.Participate(name, r => expectation.TryHandle(r), filter);
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