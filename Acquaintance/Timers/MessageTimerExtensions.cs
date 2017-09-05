using Acquaintance.PubSub;
using System;
using System.Linq;
using Acquaintance.Utility;

namespace Acquaintance.Timers
{
    public static class MessageTimerExtensions
    {
        public static IDisposable InitializeMessageTimer(this IMessageBus messageBus)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));

            return messageBus.Modules.Add(new MessageTimerModule());
        }

        public static IDisposable StartTimer(this IMessageBus messageBus, string topic, int delayMs = 5000, int intervalMs = 10000)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));

            var module = GetModule(messageBus);
            return module.AddNewTimer(topic, delayMs, intervalMs);
        }

        public static IDisposable TimerSubscribe(this IPubSubBus messageBus, string topic, int multiple, Action<IActionSubscriptionBuilder<MessageTimerEvent>> build)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.IsInRange(multiple, nameof(multiple), 1, int.MaxValue);
            Assert.ArgumentNotNull(build, nameof(build));

            // TODO: Warning to the user of the MessageTimer hasn't been added to the list of modules?

            return messageBus.Subscribe<MessageTimerEvent>(builder =>
            {
                var b2 = builder.WithTopic(topic);
                build(b2);
                var b3 = b2 as IDetailsSubscriptionBuilder<MessageTimerEvent>;
                b3?.WithFilter(t => t.Id % multiple == 0);
            });
        }

        private static MessageTimerModule GetModule(IMessageBus messageBus)
        {
            var module = messageBus.Modules.Get<MessageTimerModule>().FirstOrDefault();
            if (module == null)
                throw new Exception($"Message Timer module is not initialized. Call .{nameof(InitializeMessageTimer)}() first");
            return module;
        }
    }
}