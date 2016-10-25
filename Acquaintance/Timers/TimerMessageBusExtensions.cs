using System;

namespace Acquaintance.Timers
{
    public static class TimerMessageBusExtensions
    {
        public static IDisposable TimerSubscribe(this ISubscribable messageBus, int multiple, Action<MessageTimerEvent> subscriber, SubscribeOptions options = null)
        {
            return messageBus.Subscribe(MessageTimerEvent.EventName, subscriber, t => t.Id % multiple == 0, options);
        }
    }
}