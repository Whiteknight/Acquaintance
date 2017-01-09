﻿using Acquaintance.PubSub;
using System;

namespace Acquaintance.Timers
{
    public static class TimerMessageBusExtensions
    {
        public static IDisposable TimerSubscribe(this IPubSubBus messageBus, int multiple, Action<IActionSubscriptionBuilder<MessageTimerEvent>> build)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));
            if (multiple <= 0)
                throw new ArgumentOutOfRangeException(nameof(multiple));
            if (build == null)
                throw new ArgumentNullException(nameof(build));

            return messageBus.Subscribe<MessageTimerEvent>(builder =>
            {
                var b2 = builder.WithChannelName(MessageTimerEvent.EventName);
                build(b2);
                var b3 = b2 as IDetailsSubscriptionBuilder<MessageTimerEvent>;
                b3.WithFilter(t => t.Id % multiple == 0);
            });
        }
    }
}