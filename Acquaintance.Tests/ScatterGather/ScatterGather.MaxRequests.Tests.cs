using Acquaintance.RequestResponse;
using FluentAssertions;
using NUnit.Framework;
using System.Linq;

namespace Acquaintance.Tests.ScatterGather
{
    [TestFixture]
    public class ScatterGather_MaxRequests_Tests
    {
        private class TestResponse
        {
            public string Text { get; set; }
        }

        private class TestRequest : IRequest<TestResponse>
        {
            public string Text { get; set; }
        }

        [Test]
        public void ParticipateScatterGather_MaxRequests()
        {
            var target = new MessageBus();
            target.Participate<int, int>(l => l
                .OnDefaultChannel()
                .Invoke(req => 1)
                .Immediate()
                .MaximumRequests(2));

            target.Scatter<int, int>(1).ToList().Should().NotBeEmpty();
            target.Scatter<int, int>(2).ToList().Should().NotBeEmpty();
            target.Scatter<int, int>(3).ToList().Should().BeEmpty();
            target.Scatter<int, int>(4).ToList().Should().BeEmpty();
        }
    }
}
