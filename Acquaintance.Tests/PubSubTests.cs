using Acquaintance.Threading;
using FluentAssertions;
using NUnit.Framework;
using System.Threading;

namespace Acquaintance.Tests
{
    [TestFixture]
    public class PubSubTests
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
        public void SubscribeAndPublish()
        {
            var target = new MessageBus();
            string text = null;
            target.Subscribe<TestPubSubEvent>("Test", e => text = e.Text);
            target.Publish("Test", new TestPubSubEvent("Test2"));
            text.Should().Be("Test2");
        }

        [Test]
        public void FilteredSubscriber()
        {
            var target = new MessageBus();
            string text = null;
            target.Subscribe<TestPubSubEvent>("Test", e => text = e.Text, e => e.Text == "Test2");
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
                string text = null;
                var testThread = Thread.CurrentThread.ManagedThreadId;
                target.Subscribe<TestPubSubEvent>("Test", e => text = e.Text + Thread.CurrentThread.ManagedThreadId, new SubscribeOptions
                {
                    DispatchType = DispatchThreadType.AnyWorkerThread,
                });
                target.Publish("Test", new TestPubSubEvent("Test"));
                // TODO: Find a better way to test without hard-coded timeout.
                Thread.Sleep(2000);

                text.Should().NotBeNull();
                text.Should().NotBe("Test");
                text.Should().NotBe("Test" + testThread);
            }
            finally
            {
                target.Dispose();
            }
        }

        [Test]
        public void SubscribeAndPublish_Object()
        {
            var target = new MessageBus();
            string text = null;
            target.Subscribe<TestPubSubEvent>("Test", e => text = e.Text);
            target.Publish("Test", typeof(TestPubSubEvent), new TestPubSubEvent("Test2"));
            text.Should().Be("Test2");
        }
    }
}