using System;
using Acquaintance.Utility;

namespace Acquaintance.PubSub
{
    /// <summary>
    /// IPublishableMessage encapsulates a publish event with topic and payload into a single
    /// object, and apply it to the message bus later.
    /// </summary>
    public interface IPublishableMessage
    {
        void PublishTo(IPublishable messageBus);
    }

    public class PublishableMessage : IPublishableMessage
    {
        private readonly string _topic;
        private readonly object _message;
        private readonly Type _messageType;

        public PublishableMessage(string topic, object message)
        {
            _topic = topic;
            _message = message;
            _messageType = message.GetType();
        }

        public PublishableMessage(string topic, Type messageType, object message)
        {
            Assert.ArgumentNotNull(messageType, nameof(messageType));
            Assert.IsInstanceOf(messageType, message, nameof(message));

            _topic = topic;
            _message = message;
            _messageType = messageType;
        }

        public void PublishTo(IPublishable messageBus)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));

            messageBus.Publish(_topic, _messageType, _message);
        }
    }

    public class PublishableMessage<TPayload> : IPublishableMessage
    {
        private readonly string _topic;
        private readonly TPayload _payload;

        public PublishableMessage(string topic, TPayload payload)
        {
            _topic = topic;
            _payload = payload;
        }

        public void PublishTo(IPublishable messageBus)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));

            messageBus.Publish(_topic, _payload);
        }
    }
}
