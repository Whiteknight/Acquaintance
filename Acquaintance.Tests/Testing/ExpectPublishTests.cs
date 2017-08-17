using Acquaintance.Testing;
using FluentAssertions;
using NUnit.Framework;
using System;

namespace Acquaintance.Tests.Testing
{
    [TestFixture]
    public class ExpectPublishTests
    {
        [Test]
        public void ExpectPublish_Test()
        {
            var messageBus = new MessageBus();
            messageBus.InitializeTesting();
            messageBus.ExpectPublish<int>(null);

            messageBus.Publish(5);

            messageBus.VerifyAllExpectations();
        }

        [Test]
        public void ExpectPublish_Failed()
        {
            var messageBus = new MessageBus();
            messageBus.InitializeTesting();
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
            var messageBus = new MessageBus();
            messageBus.InitializeTesting();
            int value = 0;
            messageBus.ExpectPublish<int>(null).Callback(e => value = e);

            messageBus.Publish(5);

            value.Should().Be(5);
        }
    }
}
