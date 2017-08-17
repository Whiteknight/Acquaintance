using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.RequestResponse
{
    [TestFixture]
    public class RequestResponse_Router_Tests
    {
        [Test]
        public void RequestRouter_Route()
        {
            var target = new MessageBus();
            target.Listen<int, int>(l => l
                .WithTopic("Evens")
                .Invoke(e => e * 10));
            target.Listen<int, int>(l => l
                .WithTopic("Odds")
                .Invoke(e => e * 100));

            target.SetupRequestRouting<int, int>("", r => r
                .When(e => e % 2 == 0, "Evens")
                .When(e => e % 2 == 1, "Odds"));

            target.RequestWait<int, int>(1).Should().Be(100);
            target.RequestWait<int, int>(2).Should().Be(20);
            target.RequestWait<int, int>(3).Should().Be(300);
            target.RequestWait<int, int>(4).Should().Be(40);
            target.RequestWait<int, int>(5).Should().Be(500);
        }

        [Test]
        public void RequestRouter_DefaultRoute()
        {
            var target = new MessageBus();
            target.Listen<int, int>(l => l
                .WithTopic("Evens")
                .Invoke(e => e * 10));
            target.Listen<int, int>(l => l
                .WithTopic("Odds")
                .Invoke(e => e * 100));

            target.SetupRequestRouting<int, int>("", r => r
                .When(e => e % 2 == 0, "Evens")
                .Else("Odds"));

            target.RequestWait<int, int>(1).Should().Be(100);
            target.RequestWait<int, int>(2).Should().Be(20);
            target.RequestWait<int, int>(3).Should().Be(300);
            target.RequestWait<int, int>(4).Should().Be(40);
            target.RequestWait<int, int>(5).Should().Be(500);
        }

        [Test]
        public void RequestRouter_Distribute()
        {
            var target = new MessageBus();
            target.Listen<int, int>(l => l
                .WithTopic("A")
                .Invoke(e => e * 10));
            target.Listen<int, int>(l => l
                .WithTopic("B")
                .Invoke(e => e * 100));

            target.SetupRequestDistribution<int, int>("", new [] { "A", "B" });

            var first = target.RequestWait<int, int>(1);
            var second = target.RequestWait<int, int>(1);
            first.Should().NotBe(second);
        }
    }
}
