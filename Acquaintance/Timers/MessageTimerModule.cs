using System;
using System.Collections.Concurrent;

namespace Acquaintance.Timers
{
    public class MessageTimerModule : IMessageBusModule
    {
        private readonly ConcurrentDictionary<Guid, MessageTimer> _timers;

        private IMessageBus _messageBus;

        public MessageTimerModule()
        {
            _timers = new ConcurrentDictionary<Guid, MessageTimer>();
        }

        public void Attach(IMessageBus messageBus)
        {
            if (_messageBus != null)
                throw new Exception("MessageTimer is already attached");
            _messageBus = messageBus;
        }

        public void Start()
        {
            if (_messageBus == null)
                throw new Exception("Cannot Start when unattached");
        }

        public void Stop()
        {
            foreach (var timer in _timers.Values)
                timer.Dispose();
            _timers.Clear();
        }

        public void Unattach()
        {
            Stop();
            _messageBus = null;
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

        public void Dispose()
        {
            Stop();
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