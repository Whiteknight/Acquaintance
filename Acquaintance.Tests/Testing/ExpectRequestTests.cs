using System;
using Acquaintance.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.Testing
{
    [TestFixture]
    public class ExpectRequestTests
    {
        private IMessageBus CreateTarget()
        {
            var messageBus = new MessageBus();
            messageBus.InitializeTesting();
            return messageBus;
        }

        [Test]
        public void ExpectRequest_WillReturnConstant()
        {
            var target = CreateTarget();
            target.ExpectRequest<int, int>(null).WillReturn(5);

            var result = target.RequestWait<int, int>(4);
            result.Should().Be(5);

            target.VerifyAllExpectations();
        }

        [Test]
        public void ExpectRequest_WillReturnFactory()
        {
            var target = CreateTarget();
            target.ExpectRequest<int, int>(null).WillReturn(x => x + 5);

            var result = target.RequestWait<int, int>(4);
            result.Should().Be(9);

            target.VerifyAllExpectations();
        }

        [Test]
        public void ExpectRequest_Callback()
        {
            var target = CreateTarget();
            int value = 0;
            target.ExpectRequest<int, int>(null).WillReturn(5).Callback((req, res) => value = req + res);

            target.Request<int, int>(4).WaitForResponse();

            value.Should().Be(9);
        }

        [Test]
        public void ExpectRequest_UnmetExpectation()
        {
            var target = CreateTarget();
            target.ExpectRequest<int, int>(null);

            Action act = () => target.VerifyAllExpectations();
            act.ShouldThrow<ExpectationFailedException>();
        }
    }
}
