using System;

namespace Acquaintance.PubSub
{
    /// <summary>
    /// IPublishableMessage encapsulates a publish event with channel and payload into a single
    /// object, and apply it to the message bus later.
    /// </summary>
    public interface IPublishableMessage
    {
        void PublishTo(IPubSubBus messageBus);
    }

    public class PublishableMessage : IPublishableMessage
    {
        private readonly string _channelName;
        private readonly object _message;
        private readonly Type _messageType;

        public PublishableMessage(string channelName, object message)
        {
            _channelName = channelName;
            _message = message;
            _messageType = message.GetType();
        }

        public PublishableMessage(string channelName, Type messageType, object message)
        {
            if (messageType == null)
                throw new ArgumentNullException(nameof(messageType));
            if (!messageType.IsInstanceOfType(message))
                throw new ArgumentException("message is not an instance of the provided type", nameof(messageType));
            _channelName = channelName;
            _message = message;
            _messageType = messageType;
        }

        public void PublishTo(IPubSubBus messageBus)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));

            messageBus.Publish(_channelName, _messageType, _message);
        }
    }

    public class PublishableMessage<TPayload> : IPublishableMessage
    {
        private readonly string _channelName;
        private readonly TPayload _payload;

        public PublishableMessage(string channelName, TPayload payload)
        {
            _channelName = channelName;
            _payload = payload;
        }

        public void PublishTo(IPubSubBus messageBus)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));

            messageBus.Publish(_channelName, _payload);
        }
    }
}
