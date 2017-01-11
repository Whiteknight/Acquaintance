using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests
{
    [TestFixture]
    public class RequestRouterTests
    {
        [Test]
        public void RequestRouter_Publish()
        {
            var target = new MessageBus();
            target.Listen<int, int>(l => l
                .WithChannelName("Evens")
                .InvokeFunction(e => e * 10));
            target.Listen<int, int>(l => l
                .WithChannelName("Odds")
                .InvokeFunction(e => e * 100));

            target.Listen<int, int>(l => l
                .OnDefaultChannel()
                .Route(r => r
                    .When(e => e % 2 == 0, "Evens")
                    .When(e => e % 2 == 1, "Odds")));

            target.Request<int, int>(1).Should().Be(100);
            target.Request<int, int>(2).Should().Be(20);
            target.Request<int, int>(3).Should().Be(300);
            target.Request<int, int>(4).Should().Be(40);
            target.Request<int, int>(5).Should().Be(500);
        }
    }
}
