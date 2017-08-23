using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.ScatterGather
{
    [TestFixture]
    public class ScatterGather_Router_Tests
    {
        [Test]
        public void ScatterRouter_Route()
        {
            var target = new MessageBus();
            target.Participate<int, int>(l => l
                .WithTopic("Evens")
                .Invoke(e => e * 10));
            target.Participate<int, int>(l => l
                .WithTopic("Odds")
                .Invoke(e => e * 100));

            target.SetupScatterRouting<int, int>("", r => r
                .When(e => e % 2 == 0, "Evens")
                .When(e => e % 2 == 1, "Odds"));

            target.Scatter<int, int>(1).GatherResponses(1).First().Response.Should().Be(100);
            target.Scatter<int, int>(2).GatherResponses(1).First().Response.Should().Be(20);
            target.Scatter<int, int>(3).GatherResponses(1).First().Response.Should().Be(300);
            target.Scatter<int, int>(4).GatherResponses(1).First().Response.Should().Be(40);
            target.Scatter<int, int>(5).GatherResponses(1).First().Response.Should().Be(500);
        }

        [Test]
        public void ScatterRouter_DefaultRoute()
        {
            var target = new MessageBus();
            target.Participate<int, int>(l => l
                .WithTopic("Evens")
                .Invoke(e => e * 10));
            target.Participate<int, int>(l => l
                .WithTopic("Odds")
                .Invoke(e => e * 100));

            target.SetupScatterRouting<int, int>("", r => r
                .When(e => e % 2 == 0, "Evens")
                .Else("Odds"));

            target.Scatter<int, int>(1).GatherResponses(1).First().Response.Should().Be(100);
            target.Scatter<int, int>(2).GatherResponses(1).First().Response.Should().Be(20);
            target.Scatter<int, int>(3).GatherResponses(1).First().Response.Should().Be(300);
            target.Scatter<int, int>(4).GatherResponses(1).First().Response.Should().Be(40);
            target.Scatter<int, int>(5).GatherResponses(1).First().Response.Should().Be(500);
        }
    }
}
