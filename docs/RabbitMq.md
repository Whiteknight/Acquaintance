# RabbitMQ

Acquaintance offers an ability to transfer messages over RabbitMQ with other processes on your network, including other instances of Acquaintance.

Install the package `Acquaintance.RabbitMq`. This package uses RabbitMQ middleware to send messages which can be received by other instances of Acquaintance in other programs on other machines across your network.

Start by including a reference to the namespace to get necessary extension methods and initializing the RabbitMQ connection with a [connectionstring](https://github.com/EasyNetQ/EasyNetQ/wiki/Connecting-to-RabbitMQ):

```csharp
using Acquaintance.RabbitMq;
```

```csharp
var token = messageBus.InitializeRabbitMq("host=...;username=...;password=...");
```

### Subscribing to Remote Messages

Some other process, possibly an instance of Acquaintance, is publishing messages to Rabbit, and you would like to subscribe to those and deliver them locally. We do this with message forwarding, where we specify the message type and the topic

```csharp
var token = messageBus.PullRabbitMqToLocal<MyTestPayload>(b => b
    ...
);
```

First you should specify which Rabbit topics to listen on:

```csharp
    // Specify a topic in Rabbit to receive
    .WithRemoteTopic("remote")

    // Subscribe to the '#' wildcard
    .ForAllRemoteTopics()
```

Next you specify what topic to use when the message is published locally:

```csharp
    .ForwardToLocalTopic("local")

    .ForwardToLocalTopic(remote => "local")
```

Specify which queue to use. The name of the queue will determine whether multiple instances of Acquaintance are arranged as Parallel Consumers or Competing Consumers:

```csharp
    // Generate a unique name for this instance, so messages are not shared
    .UseUniqueQueueName()

    // Generate a queue name without instance information, intended for competing consumers
    .UseSharedQueueName()

    // Use a specific queue name. Any other instances using this queue name will compete for messages
    .UseQueueName("...")
```

Control if the message needs to be transformed from a network message to a local `Envelope<T>`. If you do nothing, Acquaintance assumes you've used it's internal network envelope types and will transform from that.

```csharp
    // Receive the message as-is and wrap it in an Envelope<T>
    .ReceiveRawMessage()

    // Specify a transformation from the remote message to a local Envelope<T>
    .TransformFrom<MyRemoteMessage>(m => ...)
```

Finally you can set some other options on the queue

```csharp
    // Expire the queue automatically when this subscription shuts down
    .AutoExpireQueue()

    // Expire the queue if there are no consumers attached after the specified number of milliseconds
    .ExpireQueue(1000)
```


### Publishing Local Messages to Rabbit

We can use a special Subscriber to subscribe to messages published locally and forward them on to Rabbit. To set this up, we go through the normal subscription building process:

```csharp
var token = messageBus.Subscribe<MyTestPayload>(b => b
    .WithTopic("send")
    .ForwardToRabbitMq());
```