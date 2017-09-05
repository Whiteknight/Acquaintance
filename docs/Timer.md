## Message Timer Module

The Message Timer module sets up a timer with the operating system. At each tick of the timer, a `MessageTimerEvent` is published. Interested components can subscribe to these timer messages to perform work at regular intervals.

Start by initializing the Timer module:

```csharp
using Acquaintance.Timers;

var token = messageBus.InitializeMessageTimer();
```

### Start a Timer

Every timer is associated with a topic and will publish a `MessageTimerEvent` message on that topic. To create a new timer specify the topic, the millisecond delay before the timer starts ticking and the interval milliseconds between ticks:

```csharp
var token = messageBus.StartTimer("topic", delayMs, intervalMs);
```

If you dispose the timer token, that timer will stop ticking and you won't receive any more messages from the timer on that topic. Existing subscriptions to that topic will remain in their respective channels until removed using their own tokens.

### Subscribe to Timer Messages

You can subscribe to the timer messages easily with a special method:

```csharp
var token = messageBus.TimerSubscribe("topic", n, builder => { ... });
```

The `n` parameter is the interval multiple to use. For instance if the timer is set for 100ms and you only want to receive a message every 300ms, you set the multiple to 3. 

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

Keep in mind that timers consume system resources and having too many timers may cause problems in your application. If possible, have fewer timers and use the multiples to limit the number you receive.