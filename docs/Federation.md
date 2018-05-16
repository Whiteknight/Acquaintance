# Federation

**Warning**: Federation is currently an experimental feature in active development.

Federation is where multiple instances of Acquaintance can communicate, such as over a network. For Federation to happen, we must have an adaptor for the specific middleware or communications mechanism in your network.

Federation options are in development and will be released as separate add-in libraries when they are available.

## Envelopes and Local Delivery

`IMessageBus` defines an `Id` property which contains a unique identifier for the messageBus instance. You can set a custom Id in the `MessageBusCreateParameters.Id` property. Otherwise a random identifier will be assigned. For federation, you should use an Id which is unique in the network but consistent for the instance. This way, the Acquaintance instance can be uniquely identified across the network and the process can continue operation if it stops and restarts.

For a single instance of Acquaintance the Id is not particularly important, but for Federation over a network it does become critical. The Id allows you to communicate with specific Acquaintance instances, or to prevent a single instance from consuming its own messages. Differing Id and other feature allow patterns like Parallel Consumers or Competing Consumers.

`Envelope<T>` defines a property `OriginBusId` which is the ID of the Acquaintance `IMessageBus` where the message was first created. Using this property, we can determine where a message originated and possibly filter out messages which are coming from undesirable sources. Some federation mechanisms may offer the option to filter out messages sent by the current Acquaintance message bus, so we don't double-deliver messages to local subscribers.

