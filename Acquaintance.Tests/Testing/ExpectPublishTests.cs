using Acquaintance.Testing;
using FluentAssertions;
using NUnit.Framework;
using System;

namespace Acquaintance.Tests
{
    [TestFixture]
    public class ExpectPublishTests
    {
        [Test]
        public void ExpectPublish_Test()
        {
            var messageBus = new MessageBus();
            messageBus.ExpectPublish<int>(null);

            messageBus.Publish(5);

            messageBus.VerifyAllExpectations();
        }

        [Test]
        public void ExpectPublish_Failed()
        {
            var messageBus = new MessageBus();
            messageBus.ExpectPublish<int>(null);

            Action act = () => messageBus.VerifyAllExpectations();
            act.ShouldThrow<ExpectationFailedException>();
        }

        [Test]
        public void ExpectPublish_Callback()
        {
            var messageBus = new MessageBus();
            int value = 0;
            messageBus.ExpectPublish<int>(null).Callback(e => value = e);

            messageBus.Publish(5);

            value.Should().Be(5);
        }
    }
}
