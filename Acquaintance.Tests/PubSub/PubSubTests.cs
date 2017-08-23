using System;
using System.Threading;
using Acquaintance.PubSub;
using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.PubSub
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
        public void Subscribe_SubscriptionBuilder_Object()
        {
            var target = new MessageBus();
            string text = null;
            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithTopic("Test")
                .Invoke(e => text = e.Text)
                .Immediate());
            target.Publish("Test", typeof(TestPubSubEvent), new TestPubSubEvent("Test2"));
            text.Should().Be("Test2");
        }

        [Test]
        public void SubscribeAndPublish_Wildcards()
        {
            var target = new MessageBus(MessageBusCreateParameters.Default.WithWildcards());
            int count = 0;
            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithTopic("1.X.c")
                .Invoke(e => count += 1)
                .Immediate());
            target.Subscribe<TestPubSubEvent>(
                builder => builder
                .WithTopic("1.Y.c")
                .Invoke(e => count += 10)
                .Immediate());
            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithTopic("1.Y.d")
                .Invoke(e => count += 100)
                .Immediate());
            target.Publish("1.*.c", new TestPubSubEvent("Test2"));
            count.Should().Be(11);
        }

        [Test]
        public void SubscribeAndPublish_Wildcards_Unsubscribe()
        {
            var target = new MessageBus(MessageBusCreateParameters.Default.WithWildcards());
            int count = 0;
            var token = target.Subscribe<TestPubSubEvent>(builder => builder
                .WithTopic("1.X.c")
                .Invoke(e => count += 1)
                .Immediate());
            token.Dispose();
            target.Publish("1.*.c", new TestPubSubEvent("Test2"));
            count.Should().Be(0);
        }

        [Test]
        public void SubscribeAndPublish_TrieStrategy_Dispose()
        {
            var target = new MessageBus(MessageBusCreateParameters.Default.WithWildcards());
            target.Dispose();
        }

        [Test]
        public void Subscribe_SubscriptionBuilder()
        {
            var target = new MessageBus();
            string text = null;
            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithTopic("Test")
                .Invoke(e => text = e.Text)
                .Immediate());
            target.Publish("Test", new TestPubSubEvent("Test2"));
            text.Should().Be("Test2");
        }

        [Test]
        public void Subscribe_Unsubscribe()
        {
            var target = new MessageBus();
            string text = null;
            var token = target.Subscribe<string>(builder => builder
                .WithDefaultTopic()
                .Invoke(e => text = e)
                .Immediate());
            target.Publish("Test2");
            text.Should().Be("Test2");

            token.Dispose();
            target.Publish("Test3");
            text.Should().Be("Test2");
        }

        [Test]
        public void Subscribe_SubscriptionBuilder_Distribute()
        {
            int a = 0;
            int b = 0;
            int c = 0;

            var target = new MessageBus();
            target.Subscribe<int>(builder => builder
                .WithTopic("a")
                .Invoke(x => a += x)
                .Immediate());
            target.Subscribe<int>(builder => builder
                .WithTopic("b")
                .Invoke(x => b += x)
                .Immediate());
            target.Subscribe<int>(builder => builder
                .WithTopic("c")
                .Invoke(x => c += x)
                .Immediate());

            target.SetupPublishDistribution<int>("", new[] { "a", "b", "c" });

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


            public void Handle(Envelope<int> message)
            {
                Sum += message.Payload;
            }
        }

        [Test]
        public void Subscript_SubscriptionBuilder_Handler()
        {
            var handler = new TestHandler();
            var target = new MessageBus();
            target.Subscribe<int>(b => b
                .WithDefaultTopic()
                .Invoke(handler)
                .Immediate());

            target.Publish(1);
            target.Publish(2);
            target.Publish(3);
            target.Publish(4);
            target.Publish(5);

            handler.Sum.Should().Be(15);
        }

        public class TestService
        {
            public int Multiply(int i)
            {
                return i * i;
            }
        }

        [Test]
        public void SubscriptionBuilder_ActivateAndInvoke()
        {
            var target = new MessageBus();
            int result = 0;
            target.Subscribe<int>(b => b
                .WithDefaultTopic()
                .ActivateAndInvoke(i => new TestService(), (service, i) => result = service.Multiply(i))
                .Immediate());

            target.Publish(5);
            result.Should().Be(25);
        }

        [Test]
        public void SubscribeAndPublish_ModifySubscription()
        {
            var target = new MessageBus();
            int x = 0;
            target.Subscribe<int>(builder => builder
                .WithTopic("Test")
                .Invoke(e => x += e)
                .Immediate()
                .ModifySubscription(s => new MaxEventsSubscription<int>(s, 3)));
            for (int i = 1; i < 100000; i *= 10)
                target.Publish("Test", i);
            x.Should().Be(111);
        }

        [Test]
        public void SubscribeAndPublish_InvokeEnvelope()
        {
            var target = new MessageBus();
            string text = null;
            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithTopic("Test")
                .InvokeEnvelope(e => text = e.Payload.Text)
                .Immediate());
            target.Publish("Test", new TestPubSubEvent("Test2"));
            text.Should().Be("Test2");
        }

        [Test]
        public void RunEventLoop_Test()
        {
            var target = new MessageBus();
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var result = 0;
            target.Subscribe<int>(b => b.WithDefaultTopic().Invoke(i => result = i).OnThread(threadId));
            target.Publish(5);
            result.Should().Be(0);
            int iterations = 0;
            target.RunEventLoop(() => iterations++ != 0);
            result.Should().Be(5);
        }

        private class ShouldUnsubscribeSubscription : ISubscription<int>
        {
            private readonly Action _act;

            public ShouldUnsubscribeSubscription(Action act)
            {
                _act = act;
            }

            public void Publish(Envelope<int> message)
            {
                _act();
            }

            public bool ShouldUnsubscribe => true;
            public Guid Id { get; set; }
        }

        [Test]
        public void Subscription_ShouldUnsubscribe()
        {
            var target = new MessageBus();
            int value = 0;
            target.Subscribe("", new ShouldUnsubscribeSubscription(() => value = 1));
            target.Publish("", 5);
            value.Should().Be(0);
        }
    }
}