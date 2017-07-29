using Acquaintance.Testing;
using FluentAssertions;
using NUnit.Framework;
using System;

namespace Acquaintance.Tests.Testing
{
    [TestFixture]
    public class ExpectPublishTests
    {
        private IMessageBus CreateTarget()
        {
            var messageBus = new MessageBus();
            messageBus.InitializeTesting();
            return messageBus;
        }

        [Test]
        public void ExpectPublish_Test()
        {
            var messageBus = CreateTarget();
            messageBus.ExpectPublish<int>(null);

            messageBus.Publish(5);

            messageBus.VerifyAllExpectations();
        }

        [Test]
        public void ExpectPublish_Failed()
        {
            var messageBus = CreateTarget();
            messageBus.ExpectPublish<int>(null);

            Action act = () => messageBus.VerifyAllExpectations();
            act.ShouldThrow<ExpectationFailedException>();
        }

        [Test]
        public void ExpectPublish_Failed_OnError()
        {
            var messageBus = new MessageBus();
            messageBus.InitializeTesting();
            messageBus.ExpectPublish<int>(null);

            string[] errors = null;
            messageBus.VerifyAllExpectations(s => errors = s);
            errors.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void ExpectPublish_Callback()
        {
            var messageBus = CreateTarget();

            int value = 0;
            messageBus.ExpectPublish<int>(null).Callback(e => value = e);

            messageBus.Publish(5);

            value.Should().Be(5);
        }
    }
}
