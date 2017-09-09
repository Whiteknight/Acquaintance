# Event Sources Module

**Warning**: Event sources are experimental and may change in future releases based on usage and feedback.

Acquaintance provides a mechanism to monitor a source of events and publish them at regular intervals. This mechanism is called "Event Sources". An Event Source is a callback which is invoked at regular intervals on a dedicated thread. The event source callback will do work and will be able to publish messages to the bus as they are available.

## Event Source Callbacks

```csharp
var token = messageBus.RunEventSource(context => {
    // Publish as many messages per iteration as you need
    context.Publish<MyEvent>("topic", new MyEvent { ... });

    // Specify a time delay before the next iteration (defaults to 1s)
    context.IterationDelayMs = 5000;

    // Mark the event source complete so that it stops iterating
    context.Complete();
});
```

## IEventSource

You can create an object to serve as an event source, and that object will be kept alive by the system so that it can hold state between calls:

```csharp
public class MyEventSource : IEventSource
{
    public void CheckForEvents(
        IEventSourceContext context,
        CancellationToken cancellationToken)
    {
        ...
    }
}
```

You can register your source object with the system:

```csharp
var token = messageBus.RunEventSource(new MyEventSource());
```