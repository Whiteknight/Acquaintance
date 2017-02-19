using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.PubSub
{
    public class PubSub_Wrap_Tests
    {
        [Test]
        public void Subscribe_SubscriptionBuilder()
        {
            var target = new MessageBus();
            string text = null;

            var act = target.WrapAction<string>(e => text = e, b => b.Immediate()).Action;
            act("Test2");
            text.Should().Be("Test2");
        }
    }
}
