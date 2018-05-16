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
var token = messageBus.ForwardRabbitMqToLocal<MyTestPayload>("send", "received");
```

### Publishing Local Messages to Rabbit

We can use a special Subscriber to subscribe to messages published locally and forward them on to Rabbit. To set this up, we go through the normal subscription building process:

```csharp
var token = messageBus.Subscribe<MyTestPayload>(b => b
    .WithTopic("send")
    .ForwardToRabbitMq());
```