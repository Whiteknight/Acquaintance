namespace Acquaintance.Nets
{
    public class Net
    {
        public const string NetworkInputChannelName = "NetworkInput";
        private readonly IMessageBus _messageBus;

        public Net(IMessageBus messageBus)
        {
            _messageBus = messageBus;
        }

        public void Inject<T>(T payload)
        {
            _messageBus.Publish<T>(NetworkInputChannelName, payload);
        }
    }
}
