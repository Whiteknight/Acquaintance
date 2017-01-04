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
            target.Listen<int, int>("Evens", e => e * 10);
            target.Listen<int, int>("Odds", e => e * 100);

            target.RequestRouter<int, int>(string.Empty)
                .Route("Evens", e => e % 2 == 0)
                .Route("Odds", e => e % 2 == 1);

            target.Request<int, int>(1).Should().Be(100);
            target.Request<int, int>(2).Should().Be(20);
            target.Request<int, int>(3).Should().Be(300);
            target.Request<int, int>(4).Should().Be(40);
            target.Request<int, int>(5).Should().Be(500);
        }
    }

    [TestFixture]
    public class ScatterRouterTests
    {
        [Test]
        public void ScatterRouter_Publish()
        {
            var target = new MessageBus();
            target.Participate<int, int>("Evens", e => e * 10);
            target.Participate<int, int>("Odds", e => e * 100);

            target.ScatterRouter<int, int>(string.Empty)
                .Route("Evens", e => e % 2 == 0)
                .Route("Odds", e => e % 2 == 1);

            target.Scatter<int, int>(1).Should().Contain(100);
            target.Scatter<int, int>(2).Should().Contain(20);
            target.Scatter<int, int>(3).Should().Contain(300);
            target.Scatter<int, int>(4).Should().Contain(40);
            target.Scatter<int, int>(5).Should().Contain(500);
        }
    }
}
