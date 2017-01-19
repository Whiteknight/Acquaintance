using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.ScatterGather
{
    [TestFixture]
    public class ScatterGather_Filter_Tests
    {
        [Test]
        public void ScatterRouter_Publish()
        {
            var target = new MessageBus();
            target.Participate<int, int>(l => l
                .OnDefaultChannel()
                .Invoke(e => e * 10)
                .Immediate()
                .WithFilter(i => i % 2 == 0));
            target.Participate<int, int>(l => l
                .OnDefaultChannel()
                .Invoke(e => e * 100)
                .Immediate()
                .WithFilter(i => i % 2 == 1));

            target.Scatter<int, int>(1).Should().Contain(100);
            target.Scatter<int, int>(2).Should().Contain(20);
            target.Scatter<int, int>(3).Should().Contain(300);
            target.Scatter<int, int>(4).Should().Contain(40);
            target.Scatter<int, int>(5).Should().Contain(500);
        }
    }
}
