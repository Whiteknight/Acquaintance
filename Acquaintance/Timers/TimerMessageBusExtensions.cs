using Acquaintance.PubSub;
using System;
using Acquaintance.Utility;

namespace Acquaintance.Timers
{
    public static class TimerMessageBusExtensions
    {
        public static IDisposable TimerSubscribe(this IPubSubBus messageBus, int multiple, Action<IActionSubscriptionBuilder<MessageTimerEvent>> build)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.IsInRange(multiple, nameof(multiple), 1, int.MaxValue);
            Assert.ArgumentNotNull(build, nameof(build));

            // TODO: Warning to the user of the MessageTimer hasn't been added to the list of modules?

            return messageBus.Subscribe<MessageTimerEvent>(builder =>
            {
                var b2 = builder.WithTopic(MessageTimerEvent.EventName);
                build(b2);
                var b3 = b2 as IDetailsSubscriptionBuilder<MessageTimerEvent>;
                b3?.WithFilter(t => t.Id % multiple == 0);
            });
        }

        public static IDisposable EnableMessageTimer(this IMessageBus messageBus, string name = null, int delayMs = 5000, int intervalMs = 10000)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));

            return messageBus.Modules.Add(new MessageTimer(name, delayMs, intervalMs));
        }
    }
}