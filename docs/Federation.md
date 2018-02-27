# Federation

**Warning**: Federation is currently an experimental feature in active development.

Federation is where multiple instances of Acquaintance can communicate, especially over a network. For Federation to happen, we must have an adaptor for the specific middleware or communications mechanism in your network.

Federation options are in development and will be released as separate add-in libraries when they are available.

## Envelopes and Local Delivery

`Envelope<T>` defines a property `LocalOnly`. When this property is set to `true` the message will only be delivered locally. If it is set to `false` the message is eligible to be sent across the network using the configured communication mechanisms.

## RabbitMQ

Install the package `Acquaintance.RabbitMq`. This package uses RabbitMQ middleware to send messages which can be received by other instances of Acquaintance in other programs on other machines across your network.

Start by initializing the RabbitMQ connection with a [connectionstring](https://github.com/EasyNetQ/EasyNetQ/wiki/Connecting-to-RabbitMQ):

```csharp
var token = messageBus.InitializeRabbit("host=...;username=...;password=...");
```

### Pub/Sub Forwarding

Now you need to configure how Acquaintance interacts with Rabbit. There are two types of interactions: Messages published in local Acquaintance can be sent to the bus, and messages from the bus can be published on local Acquaintance. First, the case where a local message is sent to the network:

```csharp
// All MyEvent objects on the given topic will be sent to Rabbit
var token = messageBus.ForwardLocalToRabbit<MyEvent>("topic");
```

Next is the case where a message from Rabbit is published on the local message bus:

```csharp
// All MyEvent objects in the Rabbit message queue will be published with the
// given topic
var token = messageBus.ForwardRabbitToLocal<MyEvent>("topic");
```
