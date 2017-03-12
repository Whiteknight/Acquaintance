using Acquaintance.PubSub;
using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.PubSub
{
    [TestFixture]
    public class PubSub_Message_Tests
    {
        [Test]
        public void PublishMessage_Generic_Test()
        {
            var bus = new MessageBus();
            int result = 0;
            bus.Subscribe<int>(b => b
                .WithChannelName("test")
                .Invoke(i => result = i)
                .Immediate());
            var target = new PublishableMessage<int>("test", 5);
            bus.PublishMessage(target);
            result.Should().Be(5);
        }

        [Test]
        public void PublishMessage_Object_Test()
        {
            var bus = new MessageBus();
            int result = 0;
            bus.Subscribe<int>(b => b
                .WithChannelName("test")
                .Invoke(i => result = i)
                .Immediate());
            var target = new PublishableMessage("test", 5);
            bus.PublishMessage(target);
            result.Should().Be(5);
        }

        [Test]
        public void PublishMessage_TypeObject_Test()
        {
            var bus = new MessageBus();
            int result = 0;
            bus.Subscribe<int>(b => b
                .WithChannelName("test")
                .Invoke(i => result = i)
                .Immediate());
            var target = new PublishableMessage("test", typeof(int), 5);
            bus.PublishMessage(target);
            result.Should().Be(5);
        }
    }
}
