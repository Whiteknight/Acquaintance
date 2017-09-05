using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading;
using Acquaintance.Timers;

namespace Acquaintance.Tests.Timers
{
    [TestFixture]
    public class MessageTimerTests
    {
        [Test]
        public void MessageTimer_Start()
        {
            var bus = new MessageBus();
            var ids = new List<long>();
            try
            {
                var moduleToken = bus.InitializeMessageTimer();
                var timerToken = bus.StartTimer("test", 100, 100);
                var subscriptionToken = bus.TimerSubscribe("test", 1, builder => builder.Invoke(mte => ids.Add(mte.Id)));

                Thread.Sleep(1000);

                moduleToken.Dispose();
                subscriptionToken.Dispose();
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
