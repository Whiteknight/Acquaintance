using Acquaintance.PubSub;
using System;
using Acquaintance.Utility;

namespace Acquaintance.Timers
{
    public static class MessageTimerExtensions
    {
        public static IDisposable InitializeMessageTimer(this IPublishable messageBus)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));

            return messageBus.Modules.Add(new MessageTimerModule(messageBus));
        }

        public static bool IsMessageTimerInitialized(this IBusBase messageBus)
        {
            var module = messageBus.Modules.Get<MessageTimerModule>();
            return module != null;
        }

        public static IDisposable StartTimer(this IBusBase messageBus, string topic, int delayMs = 5000, int intervalMs = 10000)
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
                build?.Invoke(b2);
                var b3 = b2 as IDetailsSubscriptionBuilder<MessageTimerEvent>;
                b3?.WithFilter(t => t.Id % multiple == 0);
            });
        }

        private static MessageTimerModule GetModule(IBusBase messageBus)
        {
            return messageBus.Modules.Get<MessageTimerModule>() ?? throw new Exception($"Message Timer module is not initialized. Call .{nameof(InitializeMessageTimer)}() first");
        }
    }
}