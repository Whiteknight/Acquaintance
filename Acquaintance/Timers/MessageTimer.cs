using System;
using System.Threading;

namespace Acquaintance.Timers
{
    public class MessageTimer
    {
        private readonly IMessageBus _messageBus;
        private readonly int _delayMs;
        private readonly int _intervalMs;
        private Timer _timer;
        private int _messageId;

        public MessageTimer(IMessageBus messageBus)
            : this(messageBus, 5000, 10000)
        {
        }

        public MessageTimer(IMessageBus messageBus, int delayMs, int intervalMs)
        {
            if (delayMs < 0)
                delayMs = 0;
            if (intervalMs < 100)
                throw new ArgumentOutOfRangeException(nameof(intervalMs), "intervalMs must be 100ms or higher");
            _messageBus = messageBus;
            _delayMs = delayMs;
            _intervalMs = intervalMs;
            _messageId = 0;
        }

        public void Start()
        {
            _timer = new Timer(TimerTick, null, _delayMs, _intervalMs);
        }

        public void Stop()
        {
            if (_timer == null)
                return;

            _timer.Dispose();
            _timer = null;
        }

        private void TimerTick(object state)
        {
            var id = Interlocked.Increment(ref _messageId);
            _messageBus.Publish(MessageTimerEvent.EventName, new MessageTimerEvent(id));
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
