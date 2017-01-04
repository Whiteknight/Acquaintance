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
        public void SubscribeAndPublish()
        {
            var target = new MessageBus();
            string text = null;
            target.Subscribe<TestPubSubEvent>("Test", e => text = e.Text, new SubscribeOptions
            {
                DispatchType = Threading.DispatchThreadType.Immediate
            });
            target.Publish("Test", new TestPubSubEvent("Test2"));
            text.Should().Be("Test2");
        }

        [Test]
        public void SubscribeAndPublish_Filtered()
        {
            var target = new MessageBus();
            string text = null;
            target.Subscribe<TestPubSubEvent>("Test", e => text = e.Text, e => e.Text == "Test2", new SubscribeOptions
            {
                DispatchType = Threading.DispatchThreadType.Immediate
            });
            target.Publish("Test", new TestPubSubEvent("Test1"));
            text.Should().BeNull();
            target.Publish("Test", new TestPubSubEvent("Test2"));
            text.Should().Be("Test2");
        }

        [Test]
        public void SubscribeAndPublish_FreeWorkerThread()
        {
            var target = new MessageBus(threadPool: new MessagingWorkerThreadPool(1));
            var resetEvent = new ManualResetEvent(false);
            try
            {
                target.Subscribe<TestPubSubEvent>("Test", e => resetEvent.Set(), new SubscribeOptions
                {
                    DispatchType = DispatchThreadType.AnyWorkerThread,
                });
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
        public void SubscribeAndPublish_SpecificThread()
        {
            var target = new MessageBus();
            var resetEvent = new ManualResetEvent(false);
            var id = target.StartDedicatedWorkerThread();
            try
            {

                target.Subscribe<TestPubSubEvent>("Test", e => resetEvent.Set(), SubscribeOptions.SpecificThread(id));
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
        public void SubscribeAndPublish_SpecificThread_Stopped()
        {
            var target = new MessageBus();
            var resetEvent = new ManualResetEvent(false);
            var id = target.StartDedicatedWorkerThread();
            try
            {
                target.StopDedicatedWorkerThread(id);
                target.Subscribe<TestPubSubEvent>("Test", e => resetEvent.Set(), SubscribeOptions.SpecificThread(id));
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
        public void SubscribeAndPublish_Object()
        {
            var target = new MessageBus();
            string text = null;
            target.Subscribe<TestPubSubEvent>("Test", e => text = e.Text, new SubscribeOptions
            {
                DispatchType = Threading.DispatchThreadType.Immediate
            });
            target.Publish("Test", typeof(TestPubSubEvent), new TestPubSubEvent("Test2"));
            text.Should().Be("Test2");
        }

        [Test]
        public void SubscribeAndPublish_Wildcards()
        {
            var target = new MessageBus(dispatcherFactory: new TrieDispatchStrategyFactory());
            int count = 0;
            var options = new SubscribeOptions
            {
                DispatchType = Threading.DispatchThreadType.Immediate
            };
            target.Subscribe<TestPubSubEvent>("1.X.c", e => count += 1, options);
            target.Subscribe<TestPubSubEvent>("1.Y.c", e => count += 10, options);
            target.Subscribe<TestPubSubEvent>("1.Y.d", e => count += 100, options);
            target.Publish("1.*.c", new TestPubSubEvent("Test2"));
            count.Should().Be(11);
        }

        [Test]
        public void SubscribeAndPublish_MaxEvents()
        {
            var target = new MessageBus();
            int x = 0;
            target.Subscribe<int>("Test", e => x += e, new SubscribeOptions
            {
                DispatchType = DispatchThreadType.Immediate,
                MaxEvents = 3
            });
            for (int i = 1; i < 100000; i *= 10)
                target.Publish("Test", i);
            x.Should().Be(111);
        }
    }
}