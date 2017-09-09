# The MessageBus

The `IMessageBus` object is the central component of Acquaintance. Almost all interactions with Acquaintance and all behaviors of Acquaintance are performed through the `IMessageBus`.

## Creation Parameters

## The WorkerPool

See [Threading](Threads.md)

## The Envelope Factory

## Modules

See [Modules](Modules.md)

## Tokens

Acquaintance uses the `IDisposable` pattern throughout to manage resources. Almost every operation which allocates resources will return an `IDisposable` token. Disposing the token will free all the related resources.

Almost all Tokens in Acquaintance override the `.ToString()` method to return information about what the token represents. This can be useful for debugging and auditing purposes.

Sometimes you'll want to keep track of several tokens together with a single reference. Acquaintance provides the `DisposableCollection` type for this purpose. Add tokens to a `DisposableCollection` and you can dispose all of them with a single call to `.Dispose()`:

```csharp
var collection = new DisposableCollection();

// Add tokens
collection.Add(token1);
collection.Add(token2);

// Get a string from all tokens in the collection
var report = collection.ToString()

// Dispose all tokens and free all associated resources
collection.Dispose();
```

## Segregated Buses

Consider the case of a loosely-coupled modular system. A Module can be added to the system and unloaded from the system, and Modules use the MessageBus to communicate with each other. When a module is unloaded, we want to terminate all resources of the module, including Subscribers, Listeners, Participants, Routes, Threads, etc.

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

