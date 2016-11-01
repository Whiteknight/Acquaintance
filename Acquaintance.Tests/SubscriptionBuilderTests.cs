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
            public string Text { get; set; }

            public TestPubSubEvent(string text)
            {
                Text = text;
            }
        }

        [Test]
        public void CreateSubscription()
        {
            var target = new MessageBus();
            string text = null;
            target.CreateSubscription<TestPubSubEvent>(e => text = e.Text)
                .WithChannelName("Test")
                .Subscribe();
            target.Publish("Test", new TestPubSubEvent("Test2"));
            text.Should().Be("Test2");
        }

        [Test]
        public void CreateSubscription_Filter()
        {
            var target = new MessageBus();
            string text = null;
            target.CreateSubscription<TestPubSubEvent>(e => text = e.Text)
                .WithChannelName("Test")
                .WithFilter(e => e.Text == "Test2")
                .Subscribe();

            target.Publish("Test", new TestPubSubEvent("Test1"));
            text.Should().BeNull();
            target.Publish("Test", new TestPubSubEvent("Test2"));
            text.Should().Be("Test2");
        }

        [Test]
        public void PublishOnWorkerThread()
        {
            var target = new MessageBus();
            target.StartWorkers(1);
            try
            {
                var resetEvent = new AutoResetEvent(false);
                target.CreateSubscription<TestPubSubEvent>(e => resetEvent.Set())
                    .WithChannelName("Test")
                    .OnWorkerThread()
                    .Subscribe();
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
