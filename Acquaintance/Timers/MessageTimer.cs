using System;
using System.Threading;
using Acquaintance.Utility;

namespace Acquaintance.Timers
{
    public class MessageTimer
    {
        private readonly string _topic;
        private readonly IPublishable _messageBus;
        private readonly Timer _timer;
        private int _messageId;

        public MessageTimer(IPublishable messageBus, string topic, int delayMs = 5000, int intervalMs = 10000)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            if (delayMs < 0)
                delayMs = 0;
            if (intervalMs < 100)
                throw new ArgumentOutOfRangeException(nameof(intervalMs), "intervalMs must be 100ms or higher");

            _messageBus = messageBus;

            _messageId = 0;
            _topic = topic ?? string.Empty;

            _timer = new Timer(TimerTick, null, delayMs, intervalMs);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        private void TimerTick(object state)
        {
            var bus = _messageBus;
            if (bus == null)
                return;
            var id = Interlocked.Increment(ref _messageId);
            bus.Publish(_topic, new MessageTimerEvent(_topic, id));
        }
    }
}
