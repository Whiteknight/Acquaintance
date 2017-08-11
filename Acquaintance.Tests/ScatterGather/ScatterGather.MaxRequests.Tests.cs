using FluentAssertions;
using NUnit.Framework;
using System.Linq;

namespace Acquaintance.Tests.ScatterGather
{
    [TestFixture]
    public class ScatterGather_MaxRequests_Tests
    {
        [Test]
        public void ParticipateScatterGather_MaxRequests()
        {
            var target = new MessageBus();
            target.Participate<int, int>(l => l
                .WithDefaultTopic()
                .Invoke(req => 1)
                .Immediate()
                .MaximumRequests(2));

            target.Scatter<int, int>(1).GetResponses(1).ToList().Should().NotBeEmpty();
            target.Scatter<int, int>(2).GetResponses(1).ToList().Should().NotBeEmpty();
            target.Scatter<int, int>(3).GetResponses(1).ToList().Should().BeEmpty();
            target.Scatter<int, int>(4).GetResponses(1).ToList().Should().BeEmpty();
        }
    }
}
