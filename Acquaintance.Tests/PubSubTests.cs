using Acquaintance.PubSub;
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
                .Invoke(e => text = e.Text)
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
                .Invoke(e => text = e.Text)
                .Immediate()
                .WithFilter(e => e.Text == "Test2"));
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
            var id = target.StartDedicatedWorkerThread();
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
            var id = target.StartDedicatedWorkerThread();
            try
            {
                target.StopDedicatedWorkerThread(id);
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
        public void Subscribe_SubscriptionBuilder_Object()
        {
            var target = new MessageBus();
            string text = null;
            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithChannelName("Test")
                .Invoke(e => text = e.Text)
                .Immediate());
            target.Publish("Test", typeof(TestPubSubEvent), new TestPubSubEvent("Test2"));
            text.Should().Be("Test2");
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
        public void SubscribeAndPublish_Wildcards()
        {
            var target = new MessageBus(dispatcherFactory: new TrieDispatchStrategyFactory());
            int count = 0;
            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithChannelName("1.X.c")
                .Invoke(e => count += 1)
                .Immediate());
            target.Subscribe<TestPubSubEvent>(
                builder => builder
                .WithChannelName("1.Y.c")
                .Invoke(e => count += 10)
                .Immediate());
            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithChannelName("1.Y.d")
                .Invoke(e => count += 100)
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
                .Invoke(e => x += e)
                .Immediate()
                .MaximumEvents(3));
            for (int i = 1; i < 100000; i *= 10)
                target.Publish("Test", i);
            x.Should().Be(111);
        }

        [Test]
        public void Subscribe_SubscriptionBuilder()
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
        public void Subscribe_SubscriptionBuilder_Filter()
        {
            var target = new MessageBus();
            string text = null;
            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithChannelName("Test")
                .Invoke(e => text = e.Text)
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
        public void Subscribe_SubscriptionBuilder_Distribute()
        {
            int a = 0;
            int b = 0;
            int c = 0;

            var target = new MessageBus();
            target.Subscribe<int>(builder => builder
                .WithChannelName("a")
                .Invoke(x => a += x)
                .Immediate());
            target.Subscribe<int>(builder => builder
                .WithChannelName("b")
                .Invoke(x => b += x)
                .Immediate());
            target.Subscribe<int>(builder => builder
                .WithChannelName("c")
                .Invoke(x => c += x)
                .Immediate());
            target.Subscribe<int>(builder => builder
                .OnDefaultChannel()
                .Distribute(new[] { "a", "b", "c" }));

            target.Publish(1);
            target.Publish(2);
            target.Publish(4);
            target.Publish(8);
            target.Publish(16);

            (a + b + c).Should().Be(31);
        }

        private class TestHandler : ISubscriptionHandler<int>
        {
            public int Sum { get; private set; }


            public void Handle(int payload)
            {
                Sum += payload;
            }
        }

        [Test]
        public void Subscript_SubscriptionBuilder_Handler()
        {
            var handler = new TestHandler();
            var target = new MessageBus();
            target.Subscribe<int>(b => b
                .OnDefaultChannel()
                .Invoke(handler)
                .Immediate());

            target.Publish(1);
            target.Publish(2);
            target.Publish(3);
            target.Publish(4);
            target.Publish(5);

            handler.Sum.Should().Be(15);
        }
    }
}