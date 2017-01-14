﻿using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests
{
    public class EventRouterTests
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
        public void Subscribe_SubscriptionBuilder_RouteForward()
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
    }
}
