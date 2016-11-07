using Acquaintance.Testing;
using FluentAssertions;
using NUnit.Framework;
using System;

namespace Acquaintance.Tests
{
    [TestFixture]
    public class TestingTests
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
        public void ExpectRequest__WillReturnConstant()
        {
            var target = new MessageBus();
            target.ExpectRequest<int, int>(null).WillReturn(5);

            var result = target.Request<int, int>(4);
            result.Should().Be(5);

            target.VerifyAllExpectations();
        }

        [Test]
        public void ExpectRequest_WillReturnCallback()
        {
            var target = new MessageBus();
            target.ExpectRequest<int, int>(null).WillReturn(x => x + 5);

            var result = target.Request<int, int>(4);
            result.Should().Be(9);

            target.VerifyAllExpectations();
        }

        [Test]
        public void ExpectRequest_UnmetExpectation()
        {
            var target = new MessageBus();
            target.ExpectRequest<int, int>(null);

            Action act = () => target.VerifyAllExpectations();
            act.ShouldThrow<ExpectationFailedException>();
        }
    }
}
