using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.RequestResponse
{
    [TestFixture]
    public class RequestResponseStressTests
    {
        [Test]
        public void RequestResponseStress_Test()
        {
            const int numEvents = 100000;
            var target = new MessageBus();
            target.Listen<int, int>(l => l.WithChannelName("Test").Invoke(x => 5));
            int total = 0;
            for (int i = 0; i < numEvents; i++)
            {
                var value = target.Request<int, int>("Test", i);
                total += value;
            }

            total.Should().Be(numEvents * 5);
        }

        [Test]
        public void RequestResponseStress_Wildcards()
        {
            const int numEvents = 100000;
            var target = new MessageBus(dispatcherFactory: new TrieDispatchStrategyFactory());
            target.Listen<int, int>(l => l.WithChannelName("Test").Invoke(x => 5));
            int total = 0;
            for (int i = 0; i < numEvents; i++)
            {
                var value = target.Request<int, int>("Test", i);
                total += value;
            }

            total.Should().Be(numEvents * 5);
        }
    }
}
