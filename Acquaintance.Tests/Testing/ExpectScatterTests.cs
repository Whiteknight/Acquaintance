using Acquaintance.Testing;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Linq;

namespace Acquaintance.Tests.Testing
{
    [TestFixture]
    public class ExpectScatterTests
    {
        [Test]
        public void ExpectScatter_WillReturnConstant()
        {
            var target = new MessageBus();
            target.ExpectScatter<int, int>(null).WillReturn(5);
            target.ExpectScatter<int, int>(null).WillReturn(6);

            var result = target.Scatter<int, int>(4).ToArray();
            result.Should().BeEquivalentTo(5, 6);

            target.VerifyAllExpectations();
        }

        [Test]
        public void ExpectScatter_WillReturnFactory()
        {
            var target = new MessageBus();
            target.ExpectScatter<int, int>(null).WillReturn(x => x + 5);

            var result = target.Scatter<int, int>(4).First();
            result.Should().Be(9);

            target.VerifyAllExpectations();
        }

        [Test]
        public void ExpectScatter_Callback()
        {
            var target = new MessageBus();
            int value = 0;
            target.ExpectScatter<int, int>(null).WillReturn(x => x + 5).Callback((req, res) => value = req + res.FirstOrDefault());

            var result = target.Scatter<int, int>(4).First();
            value.Should().Be(13);
        }

        [Test]
        public void ExpectScatter_UnmetExpectation()
        {
            var target = new MessageBus();
            target.ExpectScatter<int, int>(null);

            Action act = () => target.VerifyAllExpectations();
            act.ShouldThrow<ExpectationFailedException>();
        }
    }
}
