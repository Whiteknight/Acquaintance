using FluentAssertions;
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
        public void SubscriptionRouter_Publish()
        {
            var target = new MessageBus();
            int evens = 0;
            int odds = 0;
            int all = 0;
            var options = new SubscribeOptions
            {
                DispatchType = Threading.DispatchThreadType.Immediate
            };
            target.Subscribe<TestPubSubEvent>(e => all += e.Number, options);
            target.Subscribe<TestPubSubEvent>("Evens", e => evens += e.Number, options);
            target.Subscribe<TestPubSubEvent>("Odds", e => odds += e.Number, options);

            target.SubscriptionRouter<TestPubSubEvent>(string.Empty)
                .Route("Evens", e => e.Number % 2 == 0)
                .Route("Odds", e => e.Number % 2 == 1);

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
