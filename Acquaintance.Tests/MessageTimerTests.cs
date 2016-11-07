using Acquaintance.Timers;
using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading;

namespace Acquaintance.Tests
{
    [TestFixture]
    public class MessageTimerTests
    {
        [Test]
        public void MessageTimer_Start()
        {
            var bus = new MessageBus();
            try
            {
                List<long> ids = new List<long>();
                bus.Subscribe<MessageTimerEvent>(MessageTimerEvent.EventName, mte => ids.Add(mte.Id));
                var target = new MessageTimer(100, 100);
                var token = bus.Modules.Add(target);

                Thread.Sleep(1000);

                token.Dispose();
                ids.Should().OnlyHaveUniqueItems();
                ids.Count.Should().BeGreaterOrEqualTo(5);
            }
            finally
            {
                bus.Dispose();
            }
        }
    }
}
