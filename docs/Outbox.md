# Outbox Module

**Watching**:Outbox is experimental and may change in future releases based on usage and feedback.

Messages sent through Acquaintance locally, in the same process and same memory space, are reliable. So long as the process stays up and is in good health, the messages will be delivered as expected. However, when contacting remote systems, it's possible that the message may not be able to be delivered successfully on the first attempt.

The Outbox Pattern allows Publish operations to become more reliable, by creating a local store of messages which can be held and retried periodically until they are sent successfully. The implementation of the Outbox Pattern in Acquaintance consists of two parts: 

* The `IOutbox` implementations themselves, which store messages until they can be successfully sent
* The `OutboxModule` which stores a reference to active outboxes and attempts to flush messages from each periodically

## Basic Usage 
You can instantiate and use an `IOutbox` directly without having to use the `OutboxModule`, though this will require you to schedule retries yourself. Using the `OutboxModule` can be more reliable and require less effort on your part.

First, initialize the `OutboxModule`:

```csharp
var token = messageBus.InitializeOutboxModule();
```

There are two implementations of `IOutbox` built into Acquaintance: Passthrough and InMemory. To create an instance of either, you need to provide a callback for where the outbox attempts to send messages:

```csharp
// Stores a limited number of messages in a memory buffer until they can be sent succesfully
var inMemoryOutbox = messageBus.GetInMemoryOutboxFactory()
    .Create(m => MySend(m));

// Does not store messages at all, but only makes a best-effort attempt
// to send the message once
var passthruOutbox = messageBus.GetPassthroughOutboxFactory()
    .Create(m => MySend(m));
```

## Subscriptions

Request/Response and Scatter/Gather require immediate responses and so do not work with Outbox. However, Pub/Sub does easily work with Outbox to help improve deliverability of critical messages. You can add extension methods to the `SubscriptionBuilder` by adding this include to your code:

```csharp
using Acquaintance.Outbox;
```

This gives you two new methods on `SubscriptionBuilder`. The first method, `.SendToOutbox()` sends the message to the outbox but does not control where the message goes after that:

```csharp
    .SendToOutbox(outbox)
```

The second method uses an outbox as a caching layer in a subscriber pipeline, and flushes messages down the pipeline until delivery is successful:

```csharp
    .UseOutbox(outboxFactory)
```

## Custom Outboxes

If you would like to provide your own `IOutbox` implementation, for most cases you will also need to provide an `IOutboxFactory` implementation to create them. `IOutboxFactory` typically wraps a call to `IOutboxManager.AddOutboxToBeMonitored()`, which registers the outbox with the period task to retry. If you are not using the `OutboxModule` features or if you do not want your custom outbox to be automatically monitored, you can skip this call.

## Use Cases

Some use-cases where the Outbox pattern might be useful are:

1. When trying to reliably send messages over an unreliable network, by holding messages locally until they can be delivered successfully
1. When a slow module in your application cannot consume messages as quickly as they are published, we can hold them in an outbox until the receiver is ready for them.

### Sending Over a Network

I have a system with several services on a network which publish messages between them using a broker such as **RabbitMQ**. Sometimes my Broker server goes down, such as for routine maintenance, but I would like my application to continue normal operation until the broker comes back up, by caching undeliverable messages in memory until they can be delivered:

```csharp
messageBus.IntializeOutboxModule();
messageBus.Subscribe(b => b
    .ForTopics("SendBroker")
    .Invoke(m => SendToBroker(m))
    .OnWorker()
    .UseOutbox(messageBus.GetInMemoryOutboxFactory()));
```