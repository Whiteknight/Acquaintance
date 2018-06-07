using System;
using System.Collections.Concurrent;

namespace Acquaintance.Timers
{
    public class MessageTimerModule : IMessageBusModule
    {
        private readonly ConcurrentDictionary<string, MessageTimer> _timers;
        private readonly IPublishable _messageBus;

        public MessageTimerModule(IPublishable messageBus)
        {
            _messageBus = messageBus;
            _timers = new ConcurrentDictionary<string, MessageTimer>();
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
            topic = topic ?? string.Empty;
            var timer = new MessageTimer(_messageBus, topic, delayMs, intervalMs);
            bool ok = _timers.TryAdd(topic, timer);
            if (!ok)
            {
                timer.Dispose();
                throw new Exception($"Timer with topic '{topic}' already exists with interval={intervalMs}");
            }
            return new TimerToken(this, topic);
        }

        public bool TimerExists(string topic)
        {
            return _timers.ContainsKey(topic);
        }

        private void RemoveTimer(string topic)
        {
            bool ok = _timers.TryRemove(topic, out MessageTimer timer);
            if (ok)
                timer?.Dispose();
        }

        private class TimerToken : IDisposable
        {
            private readonly MessageTimerModule _module;
            private readonly string _topic;

            public TimerToken(MessageTimerModule module, string topic)
            {
                _module = module;
                _topic = topic;
            }

            public override string ToString()
            {
                return $"Message Timer Topic={_topic}";
            }

            public void Dispose()
            {
                _module.RemoveTimer(_topic);
            }
        }
    }
}