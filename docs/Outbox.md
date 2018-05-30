# Outbox Module

**Watching**: Outbox is experimental and may change in future releases based on usage and feedback.

Messages sent through Acquaintance locally, in the same process and same memory space, are reliable. So long as the process stays up, is in good health, has sufficient CPU to avoid thread starvation and has sufficient memory resources to hold message data, the messages will be delivered as expected. However, when contacting remote systems, it's possible that the message may not be able to be delivered successfully on the first attempt.

The **Outbox Pattern** allows Publish operations to become more reliable, by creating a local store of messages which can be held and retried periodically until they are sent successfully. The implementation of the Outbox Pattern in Acquaintance consists of two parts: 

* The `IOutbox` implementations themselves, which store messages until they can be successfully sent
* The `OutboxModule` which stores a reference to active outboxes and attempts to flush messages from each periodically

## Outboxes

An outbox provides two basic methods, `.AddMessage()` and `.TryFlush()`. The `AddMessage` method may add a method to some sort of internal queue or storage mechanism, and the `TryFlush` method attempts to send all stored messages to the destination.

There are two implementations of `IOutbox` built into Acquaintance: Passthrough and InMemory. 

### Passthrough Outbox

The Passthrough outbox does not store messages. It makes a single attempt to send the message which, if it fails, is not retried. The Passthrough outbox is used in cases where outbox behavior is not wanted, but the system requires an Outbox implementation to be provided. Most of the time this is used internally by the system as a kind of **Null Object pattern** implementation of `IOutbox`.

### InMemory Outbox

The InMemory Outbox stores messages in an in-memory data structure. This implementation is fast, but it suffers from several limitations:

1. **Non-Persistence**: If the messageBus shuts down, or the process terminates unexpectedly, the messages in the outbox are lost.
1. The size of the store is limited by the amount of available memory, and the amount of memory used will grow as the number of messages increase (up to a pre-defined limit)

### Custom Outboxes

Acquaintance doesn't currently provide any other options for an outbox with persistent storage. It should be relatively easy to implement an outbox using some kind of storage or stream mechanism such as the file system, SQLite, RabbitMQ, Kafka or similar technologies.

## OutboxModule

The OutboxModule maintains a timing mechanism and a list of monitored outboxes. On each iteration of the timing mechanism, the OutboxModule calls `.TryFlush()` on each registered outbox. In this way failed messages will automatically be retried periodically without having to do any additional manual work.

First, initialize the `OutboxModule`:

```csharp
var token = messageBus.InitializeOutboxModule();
```

Then you can use extension methods on the message bus to allocate `IOutbox` instances which are already registered with the OutboxModule:

```csharp
// Stores a limited number of messages in a memory buffer until they can be sent succesfully
var inMemoryOutboxAndToken = messageBus.GetInMemoryOutboxFactory().Create(m => MySend(m));
var inMemoryOutbox = inMemoryOutboxAndToken.Outbox;
var inMemoryToken = inMemoryOutboxAndToken.Token;

// Remove the inMemoryOutbox from monitoring
inMemoryToken.Dispose();

// Does not store messages at all, but only makes a best-effort attempt
// to send the message once
var passthruOutboxAndToken = messageBus.GetPassthroughOutboxFactory().Create(m => MySend(m));
```

If you would like to provide your own `IOutbox` implementation, for most cases you will also need to provide an `IOutboxFactory` implementation to create them. `IOutboxFactory` typically wraps a call to `IOutboxManager.AddOutboxToBeMonitored()`, which registers the outbox with the period task to retry. If you are not using the `OutboxModule` features or if you do not want your custom outbox to be automatically monitored, you can skip this call.

```csharp
var myOutbox = new MyOutbox(...);
var token = messageBus.AddOutboxToBeMonitored(myOutbox);
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