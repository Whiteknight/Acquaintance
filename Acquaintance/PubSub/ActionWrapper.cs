using System;
using Acquaintance.Utility;

namespace Acquaintance.PubSub
{
    public class ActionWrapper<TPayload>
    {
        public WrappedAction<TPayload> WrapAction(IPubSubBus messageBus, Action<TPayload> action, Action<IThreadSubscriptionBuilder<TPayload>> build)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(action, nameof(action));

            var topic = Guid.NewGuid().ToString();
            var token = Subscribe(messageBus, action, build, topic);
            var newAction = CreateAction(messageBus, topic);
            return new WrappedAction<TPayload>(newAction, token, topic);
        }

        private static Action<TPayload> CreateAction(IPubSubBus messageBus, string channelName)
        {
            void NewAction(TPayload t) => messageBus.Publish(channelName, t);
            return NewAction;
        }

        private static IDisposable Subscribe(IPubSubBus messageBus, Action<TPayload> action, Action<IThreadSubscriptionBuilder<TPayload>> build, string channelName)
        {
            var token = messageBus.Subscribe<TPayload>(b =>
            {
                var c = b.WithTopic(channelName).Invoke(action);
                build?.Invoke(c);
            });
            return token;
        }
    }
}
