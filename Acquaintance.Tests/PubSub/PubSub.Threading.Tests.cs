using Acquaintance.Threading;
using FluentAssertions;
using NUnit.Framework;
using System.Threading;

namespace Acquaintance.Tests.PubSub
{
    [TestFixture]
    public class PubSub_Threading_Tests
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
                .Invoke(e => text = e.Text)
                .Immediate());
            target.Publish("Test", new TestPubSubEvent("Test2"));
            text.Should().Be("Test2");
        }

        [Test]
        public void Subscribe_SubscriptionBuilder_OnWorkerThread()
        {
            var target = new MessageBus(new MessageBusCreateParameters
            {
                ThreadPool = new MessagingWorkerThreadPool(1)
            });
            var resetEvent = new ManualResetEvent(false);
            try
            {
                target.Subscribe<TestPubSubEvent>(builder => builder
                    .WithChannelName("Test")
                    .Invoke(e => resetEvent.Set())
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
        public void Subscribe_SubscriptionBuilder_OnDedicatedThread()
        {
            var target = new MessageBus();
            var resetEvent = new ManualResetEvent(false);
            try
            {
                var token = target.Subscribe<TestPubSubEvent>(builder => builder
                    .WithChannelName("Test")
                    .Invoke(e => resetEvent.Set())
                    .OnDedicatedThread());
                target.Publish("Test", new TestPubSubEvent("Test"));
                resetEvent.WaitOne(5000).Should().BeTrue();

                token.Dispose();
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
            var id = target.ThreadPool.StartDedicatedWorker();
            try
            {

                target.Subscribe<TestPubSubEvent>(builder => builder
                    .WithChannelName("Test")
                    .Invoke(e => resetEvent.Set())
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
            var id = target.ThreadPool.StartDedicatedWorker();
            try
            {
                target.ThreadPool.StopDedicatedWorker(id);
                target.Subscribe<TestPubSubEvent>(builder => builder
                    .WithChannelName("Test")
                    .Invoke(e => resetEvent.Set())
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
        public void SubscribeAndPublish_CurrentThread()
        {
            var target = new MessageBus();
            bool ok = false;
            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithChannelName("Test")
                .Invoke(e => ok = true)
                .OnThread(Thread.CurrentThread.ManagedThreadId));
            target.Publish("Test", typeof(TestPubSubEvent), new TestPubSubEvent("Test2"));

            target.EmptyActionQueue(1);
            ok.Should().BeTrue();
        }

        [Test]
        public void Subscribe_SubscriptionBuilder_WorkerThread()
        {
            var target = new MessageBus(new MessageBusCreateParameters
            {
                ThreadPool = new MessagingWorkerThreadPool(1)
            });
            try
            {
                var resetEvent = new AutoResetEvent(false);
                target.Subscribe<TestPubSubEvent>(builder => builder
                    .WithChannelName("Test")
                    .Invoke(e => resetEvent.Set())
                    .OnWorkerThread());
                target.Publish("Test", new TestPubSubEvent("Test"));
                resetEvent.WaitOne(2000).Should().BeTrue();
            }
            finally
            {
                target.Dispose();
            }
        }

        [Test]
        public void SubscribeAndPublish_ThreadPool()
        {
            var target = new MessageBus();
            var resetEvent = new ManualResetEvent(false);
            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithChannelName("Test")
                .Invoke(e => resetEvent.Set())
                .OnThreadPool());
            target.Publish("Test", typeof(TestPubSubEvent), new TestPubSubEvent("Test2"));

            bool ok = resetEvent.WaitOne(5000);
            ok.Should().BeTrue();
        }
    }
}