using System;
using System.Collections.Concurrent;

namespace Acquaintance.Timers
{
    public class MessageTimerModule : IMessageBusModule
    {
        private readonly ConcurrentDictionary<string, MessageTimer> _timers;

        private IMessageBus _messageBus;

        public MessageTimerModule()
        {
            _timers = new ConcurrentDictionary<string, MessageTimer>();
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
            if (_timers.ContainsKey(topic))
                throw new Exception($"Timer named {topic} already exists");
            var timer = new MessageTimer(_messageBus, topic, delayMs, intervalMs);
            bool ok = _timers.TryAdd(topic, timer);
            if (!ok)
            {
                timer.Dispose();
                return null;
            }
            return new TimerToken(this, topic, timer.Id);
        }

        public void Dispose()
        {
            Stop();
        }

        private void RemoveTimer(string topic, Guid id)
        {
            bool ok = _timers.TryGetValue(topic, out MessageTimer timer);
            if (!ok || timer == null || timer.Id != id)
                return;
            ok = _timers.TryRemove(topic, out timer);
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
                return $"Message Timer Topic={_topic}";
            }

            public void Dispose()
            {
                _module.RemoveTimer(_topic, _timerId);
            }
        }
    }
}