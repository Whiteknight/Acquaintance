using System;
using System.Collections.Concurrent;

namespace Acquaintance.Timers
{
    public class MessageTimerModule : IMessageBusModule
    {
        private readonly ConcurrentDictionary<Guid, MessageTimer> _timers;
        private readonly IMessageBus _messageBus;

        public MessageTimerModule(IMessageBus messageBus)
        {
            _messageBus = messageBus;
            _timers = new ConcurrentDictionary<Guid, MessageTimer>();
        }

        public void Start()
        {
        }

        public void Stop()
        {
            foreach (var timer in _timers.Values)
                timer.Dispose();
            _timers.Clear();
        }

        public IDisposable AddNewTimer(string topic, int delayMs = 5000, int intervalMs = 10000)
        {
            var id = Guid.NewGuid();
            var timer = new MessageTimer(_messageBus, topic, delayMs, intervalMs);
            bool ok = _timers.TryAdd(id, timer);
            if (!ok)
            {
                timer.Dispose();
                return null;
            }
            return new TimerToken(this, topic, id);
        }

        private void RemoveTimer(Guid id)
        {
            bool ok = _timers.TryRemove(id, out MessageTimer timer);
            if (ok)
                timer?.Dispose();
        }

        private class TimerToken : IDisposable
        {
            private readonly MessageTimerModule _module;
            private readonly string _topic;
            private readonly Guid _timerId;

            public TimerToken(MessageTimerModule module, string topic, Guid timerId)
            {
                _module = module;
                _topic = topic;
                _timerId = timerId;
            }

            public override string ToString()
            {
                return $"Message Timer Topic={_topic} Id={_timerId}";
            }

            public void Dispose()
            {
                _module.RemoveTimer(_timerId);
            }
        }
    }
}