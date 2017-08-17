using System;
using System.Threading;

namespace Acquaintance.Timers
{
    public class MessageTimer : IMessageBusModule
    {
        private readonly int _delayMs;
        private readonly int _intervalMs;
        private readonly string _name;

        private IMessageBus _messageBus;
        private Timer _timer;
        private int _messageId;

        public MessageTimer(string name = null, int delayMs = 5000, int intervalMs = 10000)
        {
            if (delayMs < 0)
                delayMs = 0;
            if (intervalMs < 100)
                throw new ArgumentOutOfRangeException(nameof(intervalMs), "intervalMs must be 100ms or higher");
            _delayMs = delayMs;
            _intervalMs = intervalMs;
            _messageId = 0;
            _name = name;
        }

        public void Attach(IMessageBus messageBus)
        {
            if (_messageBus != null)
                throw new Exception("MessageTimer is already attached");
            _messageBus = messageBus;
        }

        public void Unattach()
        {
            Stop();
            _messageBus = null;
        }

        public void Start()
        {
            if (_messageBus == null)
                throw new Exception("Cannot Start when unattached");
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
            var bus = _messageBus;
            if (bus == null)
                return;
            var id = Interlocked.Increment(ref _messageId);
            bus.Publish(MessageTimerEvent.EventName, new MessageTimerEvent(_name, id));
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
