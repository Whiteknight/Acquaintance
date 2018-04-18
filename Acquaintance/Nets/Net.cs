using System;
using System.Collections.Generic;
using System.Linq;
using Acquaintance.Utility;

namespace Acquaintance.Nets
{
    /// <summary>
    /// MessageBus wrapper which represents a network of independent processing nodes
    /// </summary>
    public class Net : IDisposable
    {
        public const string NetworkInputTopic = "NetworkInput";

        private readonly IMessageBus _messageBus;
        private readonly Dictionary<string, IReadOnlyList<NodeChannel>> _inputs;
        private readonly Dictionary<string, IReadOnlyList<NodeChannel>> _outputs;

        public Net(IMessageBus messageBus, Dictionary<string, IReadOnlyList<NodeChannel>> inputs, Dictionary<string, IReadOnlyList<NodeChannel>> outputs)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            _messageBus = messageBus;
            _inputs = inputs;
            _outputs = outputs;
        }

        public void Inject<T>(T payload)
        {
            _messageBus.Publish<T>(NetworkInputTopic, payload);
        }

        public void Validate()
        {
            new NetValidator().Validate(_inputs, _outputs);
        }

        public IReadOnlyList<Type> GetSupportedInputTypes()
        {
            return _inputs
                .SelectMany(kvp => kvp.Value)
                .Where(c => c.Topic == NetworkInputTopic)
                .Select(c => c.Type)
                .Distinct()
                .ToList();
        }

        public void Dispose()
        {
            _messageBus?.Dispose();
        }
    }
}
