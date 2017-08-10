using Acquaintance.Common;
using FluentAssertions;
using NUnit.Framework;
using System.Linq;

namespace Acquaintance.Tests.ScatterGather
{
    [TestFixture]
    public class ScatterRouterTests
    {
        [Test]
        public void ScatterRouter_Publish()
        {
            var target = new MessageBus();
            target.Participate<int, int>(l => l.WithTopic("Evens").Invoke(e => e * 10));
            target.Participate<int, int>(l => l.WithTopic("Odds").Invoke(e => e * 100));

            target.Participate<int, int>(l => l
                .WithDefaultTopic()
                .Route(r => r
                    .When(e => e % 2 == 0, "Evens")
                    .When(e => e % 2 == 1, "Odds")));

            target.Scatter<int, int>(1).Should().Contain(100);
            target.Scatter<int, int>(2).Should().Contain(20);
            target.Scatter<int, int>(3).Should().Contain(300);
            target.Scatter<int, int>(4).Should().Contain(40);
            target.Scatter<int, int>(5).Should().Contain(500);
        }

        [Test]
        public void ScatterRouter_DefaultRoute()
        {
            var target = new MessageBus();
            target.Participate<int, int>(l => l.WithTopic("Evens").Invoke(e => e * 10));
            target.Participate<int, int>(l => l.WithTopic("Odds").Invoke(e => e * 100));

            target.Participate<int, int>(l => l
                .WithDefaultTopic()
                .Route(r => r
                    .When(e => e % 2 == 0, "Evens")
                    .Else("Odds")));

            target.Scatter<int, int>(1).Should().Contain(100);
            target.Scatter<int, int>(2).Should().Contain(20);
            target.Scatter<int, int>(3).Should().Contain(300);
            target.Scatter<int, int>(4).Should().Contain(40);
            target.Scatter<int, int>(5).Should().Contain(500);
        }

        [Test]
        public void ScatterRouter_AllMatching()
        {
            var target = new MessageBus();
            target.Participate<int, int>(l => l.WithTopic("Evens").Invoke(e => e * 10));
            target.Participate<int, int>(l => l.WithTopic("Odds").Invoke(e => e * 100));
            target.Participate<int, int>(l => l.WithTopic("All").Invoke(e => e * 1000));

            target.Participate<int, int>(l => l
                .WithDefaultTopic()
                .Route(r => r
                    .WithMode(RouterModeType.AllMatchingRoutes)
                    .When(e => e % 2 == 0, "Evens")
                    .When(e => e % 2 == 1, "Odds")
                    .When(e => true, "All")));

            target.Scatter<int, int>(1).Sum().Should().Be(1100);
            target.Scatter<int, int>(2).Sum().Should().Be(2020);
            target.Scatter<int, int>(3).Sum().Should().Be(3300);
            target.Scatter<int, int>(4).Sum().Should().Be(4040);
            target.Scatter<int, int>(5).Sum().Should().Be(5500);
        }
    }
}