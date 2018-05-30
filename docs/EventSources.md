# Event Sources Module

**Warning**: Event sources are experimental and may change in future releases based on usage and feedback.

Acquaintance provides a mechanism to monitor a source of events and publish them to the bus at regular intervals. This mechanism is called "Event Sources". An Event Source is a callback which is invoked at regular intervals on a dedicated thread. The event source callback will do work and will be able to publish messages to the bus as they are available.

## Event Source Callbacks

There are two signatures for callbacks in an event source. The first takes a **context** object which can be used to control the operation of the event source. A context exposes the ability to publish an event to the bus, but does not expose all the methods of `IMessageBus`. The context can also control when the next iteration happens and when the event source is complete. 

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

The second signature also takes a `CancellationToken`, which you can use to abort the event source early:

```csharp
var token = messageBus.RunEventSource((context, cancellationToken) => {
    // check the token to break out of a long-running operation:
    while(!cancellationToken.IsCancellationRequested) 
    {
        ...
    }
});
```

Event sources with the context are conceptually similar to the `IEnumerable`/`IEnumerator` interfaces in core .NET. `context.Publish` is analogous to the `yield return` construct in an enumerator, and `context.Complete()` is analogous to the `yield break` construct. The primary difference is that enumerators tend to operate on a *pull* model while Event Sources operate on a *poll*/*push* model instead. 

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

## Use Cases

### Bulk DB Processing

I have an application which needs to read records from a DB and distribute those records to worker threads for fast bulk processing.

```csharp
// Setup worker threads
messageBus.Subscribe<MyDbRecord>(b => b
    .WithTopc("Process")
    .Invoke(r => Process(r))
    .OnThreadPool());

// Setup the event souce
messageBus.RunEventSource((c, t) => {
    while (!t.IsCancellationRequested) {
        var record = dataSource.GetNextRecord();
        if (record == null)
        {
            c.Complete();
            return;
        }

        messageBus.Publish("Process", record);
    }
});
```

### Polling a Webservice

I have a remote webservice whose state needs to be monitored. If the webservice responds to a ping, we can publish events that the webservice is healthy. If the webservice does not respond, we can publish an event that the service is unhealthy. I want to ping the webservice every minute. This ping process should continue indefinitely.

```csharp
messageBus.RunEventSource(c => {
    c.IterationDelayMs = 60000; // 60 secs/min * 1000 ms/sec = 60000 ms/min
    
    var result = myWebServiceGateway.SendPing();
    if (result != null)
        c.Publish(new MyWebserviceHealthMessage(isHealthy: true));
    else
        c.Publish(new MyWebserviceHealthMessage(isHealthy: false));
});
```
