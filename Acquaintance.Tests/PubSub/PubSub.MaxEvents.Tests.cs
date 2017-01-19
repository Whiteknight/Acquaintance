using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.PubSub
{
    [TestFixture]
    public class PubSub_MaxEvents_Tests
    {
        [Test]
        public void SubscribeAndPublish_MaxEvents()
        {
            var target = new MessageBus();
            int x = 0;
            target.Subscribe<int>(builder => builder
                .WithChannelName("Test")
                .Invoke(e => x += e)
                .Immediate()
                .MaximumEvents(3));
            for (int i = 1; i < 100000; i *= 10)
                target.Publish("Test", i);
            x.Should().Be(111);
        }
    }
}