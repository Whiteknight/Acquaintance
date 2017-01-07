using Acquaintance.Threading;
using FluentAssertions;
using NUnit.Framework;
using System.Threading;

namespace Acquaintance.Tests
{
    [TestFixture]
    public class SubscriptionBuilderTests
    {
        private class TestPubSubEvent
        {
            public string Text { get; }

            public TestPubSubEvent(string text)
            {
                Text = text;
            }
        }

        [Test]
        public void Subscribe_SubscriptionBuilder()
        {
            var target = new MessageBus();
            string text = null;
            target.Subscribe<TestPubSubEvent>(builder => builder
                .InvokeAction(e => text = e.Text)
                .WithChannelName("Test")
                .Immediate());
            target.Publish("Test", new TestPubSubEvent("Test2"));
            text.Should().Be("Test2");
        }

        [Test]
        public void Subscribe_SubscriptionBuilder_Filter()
        {
            var target = new MessageBus();
            string text = null;
            target.Subscribe<TestPubSubEvent>(builder => builder
                .InvokeAction(e => text = e.Text)
                .WithChannelName("Test")
                .Immediate()
                .WithFilter(e => e.Text == "Test2"));

            target.Publish("Test", new TestPubSubEvent("Test1"));
            text.Should().BeNull();
            target.Publish("Test", new TestPubSubEvent("Test2"));
            text.Should().Be("Test2");
        }

        [Test]
        public void Subscribe_SubscriptionBuilder_WorkerThread()
        {
            var target = new MessageBus(threadPool: new MessagingWorkerThreadPool(1));
            try
            {
                var resetEvent = new AutoResetEvent(false);
                target.Subscribe<TestPubSubEvent>(builder => builder
                .InvokeAction(e => resetEvent.Set())
                    .WithChannelName("Test")
                    .OnWorkerThread());
                target.Publish("Test", new TestPubSubEvent("Test"));
                resetEvent.WaitOne(2000).Should().BeTrue();
            }
            finally
            {
                target.Dispose();
            }
        }
    }
}
