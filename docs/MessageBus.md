# The MessageBus

The `IMessageBus` object is the central component of Acquaintance. Almost all interactions with Acquaintance and all behaviors of Acquaintance are performed through the `IMessageBus`, either directly or as extension methods.

It is a good idea to make use of the **Composition Root pattern** when setting up your MessageBus. It is probably a good idea to set up your MessageBus in the same place as you set up a Dependency Injection container for your application. Both the MessageBus and your DI container play similar roles in your application and will want to be handled in the same kinds of places, in the same kind of ways.

## Creation Parameters

## The WorkerPool

Acquaintance optionally creates and manages a pool of specialized worker threads to handle dispatch. Acquaintance can also be configured to use the default .NET threadpool and other sources of worker threads. Different workflows can be configured to dispatch work to different places as required.

See [Threading](Threads.md) for more details.

## Modules

The Acquaintance message bus is extensible through a module system, and many interactions with the message bus occur through module-specific extension methods. 

See [Modules](Modules.md) for more details.

## Logging

The MessageBus provides a simple logging abstraction that can be used to get insight into what is happening inside the MessageBus. Acquaintance does not use an existing logging library such as `Microsoft.Extensions.Logging` or `Common.Logging`, but it should be very simple to create an adaptor from either one of those to the Acquaintance logger.

Most logging which Acquaintance will do is for basic information (subscriptions added and removed, etc) and for errors in user code. For example, if a Subscription throws an unhandled exception, Acquaintance will catch it and log it. Either you can set up try/catch blocks in your own Subscriber or you can add a logger to get that information.

By default Acquaintance only provides two loggers: One that invokes a delegate and one that is silent. The silent one is used by default. All other implementations must be provided by the user.

To set a logger which dumps to the Debug window in VisualStudio:

```csharp
var messageBus = new MessageBus(new MessageBusCreateParameters {
    Logger = new DelegateLogger(s => System.Diagnostics.Debug.WriteLine(s))
});
```

To set your own logger:

```csharp
var messageBus = new MessageBus(new MessageBusCreateParameters {
    Logger = new MyLogger()
});
```

## Tokens

Acquaintance uses the `IDisposable` pattern throughout to manage resources. Almost every operation which allocates resources will return an `IDisposable` **token**. Disposing the token will free all the related resources created by that operation.

Almost all Tokens in Acquaintance override the `.ToString()` method to return information about what the token represents. This can be useful for debugging and auditing purposes.

**Warning**: The token `.ToString()` method provides information for debugging and auditing purposes, but the information in these strings is not reliable for determining uniqueness or for parsing out critical information in a consistent way. Do not rely on this string format for the operation of your application.

Sometimes you'll want to keep track of several tokens together with a single reference. Acquaintance provides the `DisposableCollection` type for this purpose. Add tokens to a `DisposableCollection` and you can dispose all of them with a single call to `.Dispose()`:

```csharp
var collection = new DisposableCollection();

// Add tokens
collection.Add(token1);
collection.Add(token2);

// Get a string from all tokens in the collection, separated by newlines
var report = collection.ToString()

// Dispose all tokens and free all associated resources
collection.Dispose();
```

## Modularity and Segregated Buses

Consider the case of a loosely-coupled modular system. A Module can be added to the system and unloaded from the system at any time, and Modules use the MessageBus to communicate with each other. When a module is unloaded, we want to terminate all resources of the module, including Subscribers, Listeners, Participants, Routes, Threads, etc.

The `SubscriptionCollection` is a wrapper around `DisposableCollection` and the `messageBus` which implements the majority of the `IMessageBus` interface. All resources allocated through the `SubscriptionCollection` are kept together, and when the collection is disposed, all those resources are removed from the bus at once.

```csharp
var bus = new SubscriptionCollection(messageBus);

// Setup several handlers and allocate resources
bus.Subscribe<MyEvent>(b => { ... });
bus.Listen<MyRequest, MyResponse>(b => { ... });
bus.Participate<MyRequest, MyResponse>(b => { ... });
bus.WorkerPool.StartDedicatedWorker();

// Dispose all handlers and allocated resources at once
bus.Dispose();
```

## Federation

Federation is the ability of Acquaintance to communicate with other components in your process and other processes running on the local machine and across the network. These other components and processes may be running Acquaintance as well, or might not be. Acquaintance should be able to communicate with most of these, typically through the use of an adaptor for the network or message channel. 

See [Federation](Federation.md) for more details.

## Envelopes and The Envelope Factory

Acquaintance wraps every message published and every request or scatter in an `Envelope<T>`, which follows the **Immutable Object pattern**. The envelope contains additional information and metadata about the message, which can be ignored in many cases, but which might be beneficial in others. Envelopes contain the payload object and topic information and also contain a unique ID and metadata about the message. You can create and publish envelopes manually:

```csharp
var envelope = messageBus.EnvelopeFactory.Create<MyMessage>("topic", message);
```

Once the envelope has been created and set up, you can use the envelope with all the major patterns:

```csharp
messageBus.PublishEnvelope(envelope);
var response = messageBus.RequestEnvelope<TRequest, TResponse>(envelope);
var gather = messageBus.ScatterEnvelope<TRequest, TResponse>(envelope);
```

In fact, these `*Envelope` method variants are what Acquaintance calls internally for all other Publush, Request and Scatter method variants.

### Metadata

Most fields of the envelope are immutable, but it does contain a mechanism for attaching metadata to the envelope, which can be inspected later by the recipient. Metadata is specified as a key/value pair with both key and value being strings:

```csharp
envelope.SetMetadata("key", "value");
```

Envelopes are passed between threads and the usual pattern for supporting this is the Immutable Object pattern. However metadata can and will change, so it requires explicit synchronization. Understand that the use of envelope metadata may incur a performance penalty, and may also lead to timing issues when multiple receiver threads are accessing and modifying metadata at once. 

### Envelope Use Cases

Some use-cases of envelope and metadata may include:
1. Keeping track of the unique message ID, so we can audit when and if messages are received, and how long they take to process
1. Giving indication about where the message originated from
1. In a federated system with networking, including information in the message that may affect how it was sent or received on the network
1. Attaching information to the message to tell where it comes from or what it represents, to aide in routing, auditing or debugging
