# Message Timer Module

The Message Timer module sets up and manages timers which publish `MessageTimerEvent` messages at every tick. Interested components can subscribe to these timer messages to perform work at regular intervals.

Start by initializing the Timer module:

```csharp
using Acquaintance.Timers;

var token = messageBus.InitializeMessageTimer();
```

## Start a Timer

Every timer is associated with a topic and will publish a `MessageTimerEvent` message on that topic. To create a new timer specify the topic, the millisecond delay before the timer starts ticking and the interval milliseconds between ticks:

```csharp
var token = messageBus.StartTimer("topic", delayMs, intervalMs);
```

If you dispose the timer token, that timer will stop ticking and you won't receive any more messages from the timer on that topic. Existing subscriptions to that topic will remain in their respective channels until removed using their own tokens.

The system only supports having a single timer per topic, to avoid conflicts. Calling `StartTimer` again with a duplicate topic will throw an Exception.

### Performance Caveats

The Timer module has a hard lower limit of 100ms per timer ticket. Attempting to setup a timer with a tick shorter than that will throw an exception. Often the Operating System cannot handle timers with much shorter periods, and if they can it usually involves setting up a separate high-precision timer which detracts from the performance of the entire system.

Keep in mind that timers consume system resources and having too many timers may cause problems in your application. If possible, have fewer timers and use filtering on messages to limit how often you receive the messages.

## Subscribe to Timer Messages

You can subscribe to the timer messages easily with a special method:

```csharp
var token = messageBus.TimerSubscribe("topic", multiple, builder => { ... });
```

The `multiple` parameter is the interval multiple to use. For instance if the timer is set for 100ms and you only want to receive a message every 300ms, you set the multiple to 3. By utilizing the multiple parameter, you can setup a single timer with a lowest common denominator interval, and subscribe many components to that one timer. This saves on system resources and improves overall performance.

Every message published by a timer has a sequence number which increases monotonically. These two calls are the same:

```csharp
var token = messageBus.TimerSubscribe("topic", 3, builder => builder
    .Invoke(m => DoThing(m)));

var token = messageBus.Subscribe<MessageTimerEvent>(builder => builder
    .WithTopic("topic")
    .Invoke(m => DoThing(m))
    .OnWorker()
    .WithFilter(m => m.Id % 3 == 0));
```



