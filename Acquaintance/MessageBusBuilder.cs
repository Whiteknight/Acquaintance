using Acquaintance.Logging;

namespace Acquaintance
{
    public class MessageBusBuilder
    {
        private readonly MessageBusCreateParameters _parameters;

        public MessageBusBuilder()
        {
            _parameters = new MessageBusCreateParameters();
        }

        public IMessageBus Build()
        {
            return new MessageBus(_parameters);
        }

        public MessageBusBuilder AllowTopicWildcards()
        {
            _parameters.AllowWildcards = true;
            return this;
        }

        public MessageBusBuilder UseWorkerThreads(int threads)
        {
            _parameters.NumberOfWorkers = threads;
            return this;
        }

        public MessageBusBuilder AllowMaximumQueuedMessages(int max)
        {
            _parameters.MaximumQueuedMessages = max;
            return this;
        }

        public MessageBusBuilder UseLogger(ILogger logger)
        {
            _parameters.Logger = logger;
            return this;
        }

        public MessageBusBuilder UseId(string id)
        {
            _parameters.Id = id;
            return this;
        }
    }
}