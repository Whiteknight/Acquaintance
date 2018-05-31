# Outbox Module

**Watching**: Outbox is experimental and may change in future releases based on usage and feedback.

Messages sent through Acquaintance locally, in the same process and same memory space, are reliable. So long as the process stays up, is in good health, has sufficient CPU to avoid thread starvation and has sufficient memory resources to hold message data, the messages will be delivered as expected. When contacting remote systems or external resources it's possible that the message may not be able to be delivered successfully on the first attempt for a large variety of reasons.

The **Outbox Pattern** allows Publish operations to become more reliable despite network unreliability. The Outbox pattern keeps a local cache of messages and periodically retries them until the send succeeds. The implementation of the Outbox Pattern in Acquaintance consists primarily of two parts: 

* The `IOutbox` implementations which store messages until they can be successfully sent
* The `OutboxModule` which stores a reference to active outboxes and attempts to automatically flush messages from each periodically

## Outboxes

An outbox provides two basic methods, `.AddMessage()` and `.GetNextQueuedMessage()`. The `AddMessage` method may add a method to some sort of internal queue or local storage mechanism, and the `GetNextQueuedMessages` method attempts to get the next batch of messages in the queue from the outbox.

Usually used internally, the `IOutboxSender` object contains logic for pulling messages out of the `IOutbox` and sending them to their destination. Most application of the Outbox pattern in Acquaintance will not require explicit reference to an `IOutboxSender`. The examples below will make use of this contruct, to show how messages from Outboxes are sent in case manual sending of messages is required. For most use cases, the OutboxModule should be used to handle this operation automatically.

Acquaintance only provides a single built-in `IOutbox` implementation, an in-memory outbox.

### InMemory Outbox

The `InMemoryOutbox<T>` stores messages in an in-memory data structure. This implementation is fast, but it suffers from three important limitations:

1. **Non-Persistence**: If the messageBus shuts down, or the process terminates unexpectedly, the messages in the outbox are lost.
1. The size of the store is limited by the amount of available memory, and the amount of memory used will grow as the number of messages increase (up to a pre-defined limit)
1. The outbox can only be read one message at a time. It is not currently possible to flush the outbox simultanously on multiple worker threads.

To be gentle on memory resources, default message limits tend to be very small (~100 messages). If you would like to devote more space for this purpose, make sure you specify that in the outbox constructor.

Here is an example of creating and using a simple `InMemoryOutbox` without automatic monitoring:

```csharp
var outbox = new InMemoryOutbox<T>(100);
var sender = new OutboxSender<T>(messageBus.Logger, outbox, m => MySend(m));
outbox.AddMessage(myMessage);
sender.TrySendAll();
```

### Custom Outboxes

Acquaintance doesn't currently provide any other options for an outbox with persistent storage. It should be relatively easy to implement an outbox using some kind of storage or stream mechanism such as the file system, SQLite, RabbitMQ, Kafka or similar technologies. You can see the implementation of the `InMemoryOutbox<T>` for examples for how to implement the necessary logic.

## OutboxModule

The OutboxModule maintains a timing mechanism and a list of monitored outboxes. On each iteration of the timing mechanism, the OutboxModule invokes the `IOutboxSender` for each registered outbox. In this way failed messages will automatically be retried periodically without having to do any additional manual work.

First, initialize the `OutboxModule`:

```csharp
var token = messageBus.InitializeOutboxModule();
```

### Monitor an Existing Outbox

You can provide your own `IOutbox` implementation. You can add an existing outbox to be monitored by the outbox module:

```csharp
var token = messageBus.AddOutboxToBeMonitored(myOutbox, m => MySend(m));

// Disable the monitoring (but do not affect the behavior of the outbox)
token.Dispose();
```

### Create a Simple Monitored Outbox

You can create an in-memory outbox and register it with the outbox module all in a single call to `GetMonitoredInMemoryOutbox`:

```csharp
// Stores a limited number of messages in a memory buffer until they can be sent succesfully
var inMemoryOutboxAndToken = messageBus.GetMonitoredInMemoryOutbox(m => MySend(m));
var inMemoryOutbox = inMemoryOutboxAndToken.Outbox;
var inMemoryToken = inMemoryOutboxAndToken.Token;

// Remove the inMemoryOutbox from monitoring
inMemoryToken.Dispose();
```

### An Outbox with Automatic and Manual Sending

You can create your own OutboxSender, and be able to manually trigger sends as well as have them be monitored by the outbox module:

```csharp
var outbox = new InMemoryOutbox<T>(100);
var sender = new OutboxSender<T>(messageBus.Logger, outbox, m => MySend(m));
var token = messageBus.AddOutboxToBeMonitored(sender);

outbox.AddMessage(myMessage);

// If this fails, it will be retried periodically by the OutboxModule
sender.TrySendAll();
```

## Subscriptions

Outbox is designed specifically for Pub/Sub to help improve deliverability of critical messages. You can add extension methods to the `SubscriptionBuilder` by adding this include to your code:

```csharp
using Acquaintance.Outbox;
```

This gives you a new method on the subscription builder. The `.UseOutbox()` method uses an outbox as a caching layer in a subscriber pipeline, and flushes messages down the pipeline until delivery is successful:

```csharp
    .UseOutbox(outbox)

    // Helper method to .UseOutbox with a new InMemoryOutbox<T>:
    .UseInMemoryOutbox()
```

## Request/Response and Scatter/Gather

Outbox does not work with either Request/Response or Scatter/Gather patterns because those patterns depend on a timely response, which an outbox does not guarantee.

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
    .UseOutbox(new InMemoryOutbox<MyMessage>(100)));
```

