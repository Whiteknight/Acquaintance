using Acquaintance.PubSub;
using System;

namespace Acquaintance.Timers
{
    public static class TimerMessageBusExtensions
    {
        public static IDisposable TimerSubscribe(this IPubSubBus messageBus, int multiple, Action<SubscriptionBuilder<MessageTimerEvent>> build)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));
            if (multiple <= 0)
                throw new ArgumentOutOfRangeException(nameof(multiple));
            if (build == null)
                throw new ArgumentNullException(nameof(build));

            return messageBus.Subscribe<MessageTimerEvent>(builder =>
            {
                builder
                    .WithChannelName(MessageTimerEvent.EventName)
                    .WithFilter(t => t.Id % multiple == 0);
                build(builder);
            });
        }
    }
}