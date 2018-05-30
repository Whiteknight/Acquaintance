# Modules

Modules are extension points for the Acquaintance MessageBus which add new functionality. Some of these are straight-forward applications of the existing messaging patterns, while others add completely new behavior.

Most modules consist of three parts: An `IMessageBusModule` class which is registered with the MessageBus, A series of extension methods on the MessageBus representing the API for that module, and a series of helper classes which implement the behavior.

* [Event Sources](EventSources.md)
* [Message Timer](Timer.md)
* [Sagas](Sagas.md)
* [Testing](Testing.md)
* [Outbox](Outbox.md)

## Developing Modules

A module must implement the `IMessageBusModule` interface and provide implementations of the `Start()` and `Stop()` methods. `Start()` is called when the module is added to the message bus, and `Stop()` is called when the module is being removed from the message bus, but *is not called* when the message bus is shutting down. To also have an opportunity to cleanup resources when the message bus is shutting down, you should implement `IDisposable`.

### IDisposable

If the module also implements the `IDisposable` interface, the `.Dispose()` method will be called when the message bus is shutting down. Threads allocated from the Acquaintance [worker pool](Threads.md) will automatically be reclaimed when the worker pool is disposed. Tokens for subscriptions, responders, routes and other modules will also automatically be disposed and deallocated also. The only things that your module needs to be responsible for disposing in these cases are resources which have been allocated separated from Acquaintance, such as custom threads, memory and external resources. 

### Behavior of a Module

The message bus does not expose any special API for the module. What the message bus does is maintain a reference to the module and manage its lifecycle. The module will be in charge of all other behavior and management of resources.

Most modules will provide extension methods on the `IMessageBus` type which wrap calls to methods on the module and compose operations on the message bus.

### Recommendations

Some general recommendations about developing modules is as follows:

1. Take an instance of `IMessageBus` in the constructor, it won't be passed in to the module anywhere else
1. Setup all necessary resources in the `.Start()` method
1. Cleanup all allocated resources in the `.Stop()` method
1. Implement `IDisposable` only if you must guarantee that external resources are cleaned up when the message bus shuts down
