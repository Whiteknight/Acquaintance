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
            public string Text { get; }

            public TestPubSubEvent(string text)
            {
                Text = text;
            }
        }

        [Test]
        public void Subscribe_SubscriptionBuilder_Immediate()
        {
            var target = new MessageBus();
            string text = null;
            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithChannelName("Test")
                .InvokeAction(e => text = e.Text)
                .Immediate());
            target.Publish("Test", new TestPubSubEvent("Test2"));
            text.Should().Be("Test2");
        }

        [Test]
        public void Subscribe_SubscriptionBuilder_Filtered()
        {
            var target = new MessageBus();
            string text = null;
            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithChannelName("Test")
                .InvokeAction(e => text = e.Text)
                .WithFilter(e => e.Text == "Test2")
                .Immediate());
            target.Publish("Test", new TestPubSubEvent("Test1"));
            text.Should().BeNull();
            target.Publish("Test", new TestPubSubEvent("Test2"));
            text.Should().Be("Test2");
        }

        [Test]
        public void Subscribe_SubscriptionBuilder_OnWorkerThread()
        {
            var target = new MessageBus(threadPool: new MessagingWorkerThreadPool(1));
            var resetEvent = new ManualResetEvent(false);
            try
            {
                target.Subscribe<TestPubSubEvent>(builder => builder
                    .WithChannelName("Test")
                    .InvokeAction(e => resetEvent.Set())
                    .OnWorkerThread());
                target.Publish("Test", new TestPubSubEvent("Test"));
                resetEvent.WaitOne(5000).Should().BeTrue();
            }
            finally
            {
                resetEvent.Dispose();
                target.Dispose();
            }
        }

        [Test]
        public void Subscribe_SubscriptionBuilder_OnThread()
        {
            var target = new MessageBus();
            var resetEvent = new ManualResetEvent(false);
            var id = target.StartDedicatedWorkerThread();
            try
            {

                target.Subscribe<TestPubSubEvent>(builder => builder
                    .WithChannelName("Test")
                    .InvokeAction(e => resetEvent.Set())
                    .OnThread(id));
                target.Publish("Test", new TestPubSubEvent("Test"));

                resetEvent.WaitOne(5000).Should().BeTrue();
            }
            finally
            {
                resetEvent.Dispose();
                target.Dispose();
            }
        }

        [Test]
        public void Subscribe_SubscriptionBuilder_OnThread_Stopped()
        {
            var target = new MessageBus();
            var resetEvent = new ManualResetEvent(false);
            var id = target.StartDedicatedWorkerThread();
            try
            {
                target.StopDedicatedWorkerThread(id);
                target.Subscribe<TestPubSubEvent>(builder => builder
                    .WithChannelName("Test")
                    .InvokeAction(e => resetEvent.Set())
                    .OnThread(id));
                target.Publish("Test", new TestPubSubEvent("Test"));

                resetEvent.WaitOne(1000).Should().BeFalse();
            }
            finally
            {
                resetEvent.Dispose();
                target.Dispose();
            }
        }

        [Test]
        public void Subscribe_SubscriptionBuilder_Object()
        {
            var target = new MessageBus();
            string text = null;
            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithChannelName("Test")
                .InvokeAction(e => text = e.Text)
                .Immediate());
            target.Publish("Test", typeof(TestPubSubEvent), new TestPubSubEvent("Test2"));
            text.Should().Be("Test2");
        }

        [Test]
        public void SubscribeAndPublish_Wildcards()
        {
            var target = new MessageBus(dispatcherFactory: new TrieDispatchStrategyFactory());
            int count = 0;
            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithChannelName("1.X.c")
                .InvokeAction(e => count += 1)
                .Immediate());
            target.Subscribe<TestPubSubEvent>(
                builder => builder
                .WithChannelName("1.Y.c")
                .InvokeAction(e => count += 10)
                .Immediate());
            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithChannelName("1.Y.d")
                .InvokeAction(e => count += 100)
                .Immediate());
            target.Publish("1.*.c", new TestPubSubEvent("Test2"));
            count.Should().Be(11);
        }

        [Test]
        public void SubscribeAndPublish_MaxEvents()
        {
            var target = new MessageBus();
            int x = 0;
            target.Subscribe<int>(builder => builder
                .WithChannelName("Test")
                .InvokeAction(e => x += e)
                .Immediate()
                .MaximumEvents(3));
            for (int i = 1; i < 100000; i *= 10)
                target.Publish("Test", i);
            x.Should().Be(111);
        }
    }
}