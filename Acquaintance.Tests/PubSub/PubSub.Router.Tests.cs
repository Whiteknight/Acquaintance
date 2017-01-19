using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.PubSub
{
    [TestFixture]
    public class PubSub_Router_Tests
    {
        private class TestPubSubEvent
        {
            public int Number { get; }

            public TestPubSubEvent(int number)
            {
                Number = number;
            }
        }

        [Test]
        public void Subscribe_SubscriptionBuilder_Route()
        {
            var target = new MessageBus();
            int evens = 0;
            int odds = 0;
            int all = 0;

            target.Subscribe<TestPubSubEvent>(builder => builder
                .OnDefaultChannel()
                .Invoke(e => all += e.Number)
                .Immediate());
            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithChannelName("Evens")
                .Invoke(e => evens += e.Number)
                .Immediate());
            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithChannelName("Odds")
                .Invoke(e => odds += e.Number)
                .Immediate());

            target.Subscribe<TestPubSubEvent>(builder => builder
                .OnDefaultChannel()
                .Route(r => r
                    .When(e => e.Number % 2 == 0, "Evens")
                    .When(e => e.Number % 2 == 1, "Odds")));

            target.Publish(new TestPubSubEvent(1));
            target.Publish(new TestPubSubEvent(2));
            target.Publish(new TestPubSubEvent(3));
            target.Publish(new TestPubSubEvent(4));
            target.Publish(new TestPubSubEvent(5));

            all.Should().Be(15);
            evens.Should().Be(6);
            odds.Should().Be(9);
        }

        [Test]
        public void Subscribe_SubscriptionBuilder_NoDefaultRoute()
        {
            var target = new MessageBus();
            int evens = 0;
            int all = 0;

            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithChannelName("Default")
                .Invoke(e => all += e.Number)
                .Immediate());
            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithChannelName("Evens")
                .Invoke(e => evens += e.Number)
                .Immediate());

            target.Subscribe<TestPubSubEvent>(builder => builder
                .OnDefaultChannel()
                .Route(r => r
                    .When(e => e.Number % 2 == 0, "Evens")));

            target.Publish(new TestPubSubEvent(1));
            target.Publish(new TestPubSubEvent(2));
            target.Publish(new TestPubSubEvent(3));
            target.Publish(new TestPubSubEvent(4));
            target.Publish(new TestPubSubEvent(5));

            all.Should().Be(0);
            evens.Should().Be(6);
        }

        [Test]
        public void Subscribe_SubscriptionBuilder_DefaultRoute()
        {
            var target = new MessageBus();
            int evens = 0;
            int all = 0;

            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithChannelName("Default")
                .Invoke(e => all += e.Number)
                .Immediate());
            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithChannelName("Evens")
                .Invoke(e => evens += e.Number)
                .Immediate());

            target.Subscribe<TestPubSubEvent>(builder => builder
                .OnDefaultChannel()
                .Route(r => r
                    .When(e => e.Number % 2 == 0, "Evens")
                    .Else("Default")));

            target.Publish(new TestPubSubEvent(1));
            target.Publish(new TestPubSubEvent(2));
            target.Publish(new TestPubSubEvent(3));
            target.Publish(new TestPubSubEvent(4));
            target.Publish(new TestPubSubEvent(5));

            all.Should().Be(9);
            evens.Should().Be(6);
        }

        [Test]
        public void Subscribe_SubscriptionBuilder_AllMatchingMode()
        {
            var target = new MessageBus();
            int evens = 0;
            int odds = 0;
            int all = 0;

            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithChannelName("All")
                .Invoke(e => all += e.Number)
                .Immediate());
            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithChannelName("Evens")
                .Invoke(e => evens += e.Number)
                .Immediate());
            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithChannelName("Odds")
                .Invoke(e => odds += e.Number)
                .Immediate());

            target.Subscribe<TestPubSubEvent>(builder => builder
                .OnDefaultChannel()
                .Route(r => r
                    .Mode(Common.RouterModeType.AllMatchingRoutes)
                    .When(e => e.Number % 2 == 0, "Evens")
                    .When(e => e.Number % 2 == 1, "Odds")
                    .When(e => true, "All")));

            target.Publish(new TestPubSubEvent(1));
            target.Publish(new TestPubSubEvent(2));
            target.Publish(new TestPubSubEvent(3));
            target.Publish(new TestPubSubEvent(4));
            target.Publish(new TestPubSubEvent(5));

            all.Should().Be(15);
            evens.Should().Be(6);
            odds.Should().Be(9);
        }

        [Test]
        public void Subscribe_SubscriptionBuilder_FirstMatchingMode()
        {
            var target = new MessageBus();
            int evens = 0;
            int all = 0;

            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithChannelName("All")
                .Invoke(e => all += e.Number)
                .Immediate());
            target.Subscribe<TestPubSubEvent>(builder => builder
                .WithChannelName("Evens")
                .Invoke(e => evens += e.Number)
                .Immediate());

            target.Subscribe<TestPubSubEvent>(builder => builder
                .OnDefaultChannel()
                .Route(r => r
                    .Mode(Common.RouterModeType.FirstMatchingRoute)
                    .When(e => e.Number % 2 == 0, "Evens")
                    .When(e => true, "All")));

            target.Publish(new TestPubSubEvent(1));
            target.Publish(new TestPubSubEvent(2));
            target.Publish(new TestPubSubEvent(3));
            target.Publish(new TestPubSubEvent(4));
            target.Publish(new TestPubSubEvent(5));

            all.Should().Be(9);
            evens.Should().Be(6);
        }
    }
}
