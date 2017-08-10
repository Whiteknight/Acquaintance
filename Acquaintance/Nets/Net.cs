using Acquaintance.Utility;

namespace Acquaintance.Nets
{
    /// <summary>
    /// MessageBus wrapper which represents a network of independent processing nodes
    /// </summary>
    public class Net
    {
        public const string NetworkInputChannelName = "NetworkInput";
        private readonly IMessageBus _messageBus;

        public Net(IMessageBus messageBus)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            _messageBus = messageBus;
        }

        public void Inject<T>(T payload)
        {
            _messageBus.Publish<T>(NetworkInputChannelName, payload);
        }

        // TODO: Method to Validate the net. Keep a list of all channels written to and read from.
        // If any node is reading from a channel which is not currently being read to, report a 
        // problem.
    }
}
