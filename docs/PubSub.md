# Publish/Subscribe

**Publish/Subscribe** ("Pub/Sub") is a messaging pattern where a component publishes a message on a **channel** and all **subscribers** to that channel receive a copy of the message. This is one of the core patterns of Acquaintance, and represents a substrate on which a number of other features and patterns are built.

Subscribers are added to a channel during setup, and messages can be published to a channel from anywhere in the application. As a general design philosophy, it's important to recognize that creating subscribers tends to happen relatively infrequently, during a setup or initialization phase of the application, but publishing messages tends to happen frequently at all other points of the application lifetime. For that reason, Acquaintance has been optimized to improve performance and simplicity of Publish over Subscribe.

## Channels

Acquaintance Pub/Sub Channels are defined by two pieces of information: A payload **type** and a **topic**. In Acquaintance these two pieces of information together represent a unique key for a channel.

Acquaintance has two modes of operation. The first treats topic strings as literals and makes a single match to the channel with the given topic. The second mode allows topic strings to contain **wildcards** and all channels are selected which match the pattern.

### Default Topic

The default topic for publishing, if none is provided, is the empty string. A `null` topic is coalesced to the default topic (empty string). These three calls are all equivalent:

```csharp
messageBus.Publish<int>("", 1);
messageBus.Publish<int>(null, 1);
messageBus.Publish<int>(1);
```

For subscribers the case is a bit more complex. If a subscriber subscribes to a `null` topic, it will receive messages for all topics for the given message payload type. If it subscribes to an empty string `""` or a specific topic string, it will receive messages for that case only. 

### Wildcards

If the option is enabled in the message bus, Publishers may publish to multiple channels simultaneously by providing a topic with wildcards. Acquaintance will pick all channels which match the wildcard provided. To enable wildcards, you must specify the option when you create the message bus. This changes Acquaintance to store channels in a way that supports wildcards, but also incurs a slight performance penalty:

```csharp
var messageBus = new MessageBusBuilder()
    .AllowTopicWildcards()
    .Build();
```

With wildcards enabled, topic strings are parsed by separating on periods ('.') and matching parts with an asterisk ('*').

```csharp
// Publishes to topics like 'A.B.C' and 'A.B.X'
messageBus.Publish<MyMessage>("A.B.*", message);
```

You cannot subscribe with a topic containing a wildcard. You can subscribe to all topics and filter out the ones you don't want to listen on, but this may incur a performance penalty.

## Publishing

Acquaintance is optimized so that the common operations should be fast, while the uncommon operations do not need to be. Publishing is considered to be the most common operation in the Pub/Sub pattern and so more care has been taken to optimize that pathway than others.

Publishing, as seen in examples above, can be done with a single method call and only requires specifying a message type, a topic and a message payload.

### Subtypes

As mentioned above, the payload **type** and the **topic** represent a unique key in the system. Publishing messages does not respect subtypes. Consider this class hierarchy:

```csharp
public class MessageParent { }
public class MessageChild : MessageParent { }
```

In this case, the following calls will publish to different subscribers:

```csharp
var p = new MessageParent();
var c = new MessageChild();

messageBus.Publish(p); // uses type MessageParent
messageBus.Publish(c); // uses type MessageChild
messageBus.Publish<MessageParent>(c); // uses type MessageParent
```

There is currently no way to setup a subscriber such that it receives messages for a type and all its subtypes. There are two strategies you might consider if you're looking for this kind of behavior:

1. Use a single message type with no inheritance, and use data in the message to differentiate its purpose in the subscriber
1. Always publish and subscribe explicitly on the parent type, and use the `is` operator and pattern matching in the receiver to handle messages differently based on type

### Anonymous Publish

In some situations the type of the message payload will not be known at compile time. Consider, for example, the case where a remote system is sending messages to your application encoded as JSON or XML, and your application is materializing those requests to types depending on what the payload contains (a `"$type"` property in the JSON, for example). In these cases the type of the message is not known at compile time, but you can publish the message anonymously:

```csharp
messageBus.Publish("topic", message.GetType(), message);
```

This method calls into the normal publish methods using reflection and may incur a performance penalty.

### Envelopes

By manually creating an `Envelope<T>` to wrap a message, you can modify the envelope metadata before publishing the message.

```csharp
var envelope = messageBus.EnvelopeFactory.Create<MyMessage>("topic", myMessage);
envelope.SetMetadata("key", "value");
messageBus.PublishEnvelope(envelope);
```

## Subscribing

Publishing is simple and straight-forward, but Subscribing is where the real complexity lies. As mentioned above, this is an explicit design decision of Acquaintance: Publish happens more frequently so it has been optimized more for both performance and usability. A subscription is a **Composite Object** which encapsulates a number of options and behaviors. Specifically, a subscription is typically set up like a pipeline or **Chain of Responsibility**, with each stage in the pipeline doing some work and then passing the message on to the next step. The most simple subscription method looks like this:

```csharp
messageBus.Subscribe<MyMessage>("topic", subscription);
```

This `Subscribe` method looks simple, but the `subscription` object may be quite complicated with many options and settings. For this reason, Acquaintance provides a `SubscriptionBuilder` object which can be used to build up a subscription with all of these details:

```csharp
messageBus.Subscribe<MyMessage>(builder => { });
```

By calling methods on the builder object (which supports a fluent interface for method chaining) you can get the correct behavior. First specify the topic using one of these methods:

```csharp
builder
    // specify the topic explicitly
    .WithTopic("topic")

    // Use the default topic
    .WithDefaultTopic()

    // For all topics
    .ForAllTopics()
```

Next specify an action with one of these methods:

```csharp
    // Invoke an action on the payload
    .Invoke(payload => { })

    // Invoke an action on the envelope, which gives access to metadata
    .InvokeEnvelope(envelope => { })

    // Invoke a method on a handler object of type ISubscriptionHandler<T>
    .Invoke(handler)

    // Instantiate a service to handle the message;
    .ActivateAndInvoke(
        payload => new Service(),
        (payload, service) => { ... })

    // Transform the message to a new type and publish on a new channel
    .TransformTo<MyOtherMessage>(
        payload => new MyOtherMessage(),
        "newTopic")

    // Use a custom ISubscription<T> or ISubscriberReference<T> class, if you have one
    .UseCustomSubscriber(subscriber)
```

Optionally specify how you want the action dispatched using one of the [Threading Options](Threads.md):

```csharp
    // On an Acquaintance worker thread (Default)
    .OnWorker()

    // Immediately on the publisher thread (not recommended)
    .Immediate()

    // On a specific .NET thread
    .OnThread(threadId)

    // On the .NET Threadpool (using System.Threading.Task)
    .OnThreadPool()

    // Create a new worker thread, and use only that thread for
    // this subscriber
    .OnDedicatedWorker()
```

Finally, if you still have more things to specify, you can put in a few other options:

```csharp
    // Only receive messages which satisfy a Predicate<T>
    .WithFilter(payload => true)

    // Only receive envelopes which satisfy a Predicate<Envelope<T>>
    .WithFilterEnvelope(envelope => true)

    // Only handle a limited number of events
    .MaximumEvents(5)

    // Make changes to the subscription at the front of the pipeline
    .WraoSubscriptionBase(subscription => { })

    // Make changes to the subscription object at the end of the pipeline
    .WrapSubscription(subscription => { })
```

The subscription builder uses segregated interfaces to help protect you for specifying conflicting options. At each step, only a few methods will be available to you to choose. Don't fight it. Setup things in order and you'll avoid whole classes of potential bugs.

### Unsubscribing

Acquaintance uses the Disposable object pattern for unsubscribing. Every `.Subscribe` method variant returns an `IDisposable` token. Disposing this token removes the subscription from the channel:

```csharp
// Create the subscription
var token = messageBus.Subscribe<int>(b => { ... });

// Remove the subscription
token.Dispose();
```

Disposing the subscription token removes the subscription from the channel and cleans up all related resources. If you specified options in the `SubscriptionBuilder` such as the `.OnDedicatedWorker()` option, disposing the token will also stop and cleanup the worker thread.

### Errors

Exceptions thrown from the subscriber are not passed back to the publisher thread. These exceptions are caught and logged internally to Acquaintance. If you want to see these errors, setup a logger when you create the message bus:

```csharp
var messageBus = new MessageBus(new MessageBusCreateParameters {
    Logger = ...
});
```

By default Acquaintance does not log anywhere, but you can easily create your own logger. It would be easy to adapt `Common.Logging`, `log4net` another logging tool to work with Acquaintance. For simplicity and portability, Acquaintance doesn't explicitly bind to any of these frameworks, instead allowing the developer to choose how logging happens.

For simple purposes, you can just call a delegate with log messages to dump to the console, the debug window, or a file somewhere;

```csharp
var messageBus = new MessageBus(new MessageBusCreateParameters {
    Logger = new DelegateLogger(s => System.Console.WriteLine(s));
});
```

### Reliable Publishing with Outboxes

Acquaintance provides an implementation of the [Outbox pattern](Outbox.md) to support reliable message publishing. Please see that page for more details.

### Autosubscribing

**Warning**: Autosubscribing is currently experimental and is subject to change between versions based on usage and feedback.

If you don't need fine-tuned control over the options of your subscription, and would like them to be automatically created from attributes, you can use the autosubscription mechanism.

First, create your class with public methods annotated with the `SubscriptionAttribute`:

```csharp
public class MyObject {
    [Subscription]
    public void MyMethod(MyMessage payload) { ... }

    [Subscription(Topics: new [] {"topic"})]
    public void MyMethod(MyMessage payload) { ... }

    [Subscription(Type: typeof(MyMessageType))]
    public void MyMethod(MyMessageOrSubclass payload) { ... }

    [Subscription]
    public void MyMethod(Envelope<MyMessage> envelope) { ... }
}
```

Next, you can subscribe an instance of this class with Acquaintance, which will automatically setup all necessary subscriptions:

```csharp
var myObject = new MyObject();
var token = messageBus.AutoSubscribe(myObject);
```

The method currently must be public, must not be static, must return `void` and must take a single parameter of the correct type. If the scanner detects that any of these conditions are not satisfied, it will silently skip the method. You cannot currently specify complicated options such as dispatch thread, maximum events, or anything else, though these features might be added in a later release.

Notice that Acquaintance maintains a strong reference to your object, which will prevent it from getting collected by GC. 

### Wrapping an Action

Sometimes you would like to take an existing `Action<T>` delegate and wrap it up so that Acquaintance will dispatch the action using it's normal dispatch engine.

```csharp
Action<int> original = i => Console.WriteLine(i);
WrappedAction<int> info = messageBus.WrapAction<int>(original, b => b
    .OnDedicatedThread()
    .MaximumEvents(5));

// The new delegate, which will execute the original delegate on a
// dedicated worker thread
Action<int> wrapped = info.Action;

// The subscription token. Disposing this cancels the subscription
// and causes the wrapped delegate to do nothing
IDisposable token = info.Token;

// The topic, which you can use to subscribe other listeners to the
// same action invocation
string topic = info.Topic
```

If you're passing actions around, this is a way to use Acquaintance internally without having to change your method signatures.

## Routing

Acquaintance allows you to setup routing rules on a topic, so that the message can be dispatched to other topics. The most straight-forward but least used method for this is `.AddRule()`:

```csharp
var token = messageBus.PublishRouter.AddRule("topic", rule);
```

Routing rules are resoved before Channels are selected, which means you cannot create a loop by routing a topic back to itself. 

### Predicate-Based Routing

As with subscriptions, there are better method variants which use Builder and other patterns to specify the fine-grained details:

```csharp
var token = messageBus.SetupPublishRouting<T>(b => b
    // Specify the input topic which Publishers use:
    .FromTopics("InTopic")
    .FromDefaultTopic()

    // Provide predicates. When the message matches the predicate, it is redirected from its original topic to the new topic
    .When(m => ..., "OutTopic1")
    .When(m => ..., "OutTopic2")

    // If none of the .When() predicates are matched, you can optionally specify a default topic
    .Else("DefaultOutTopic")
);
```

### Distribution

Similar to routing, you can setup a round-robin distribution rule which will pick from a list of provided topics using a round-robin algorithm:

```csharp
var token = messageBus.SetupPublishDistribution("topic", new[] {
    "TopicA",
    "TopicB",
    "TopicC"
});
```

Consider the use-case where you have a network with multiple copies of a service. You can use distribution as a form of load-balancer, to send requests to different service instances.

### Route By Examination

Sometimes the payload object contains the information needed for its own routing. You can derive the topic to use by examining the payload object:

```csharp
var token = messageBus.SetupPublishByExamination("InTopic",
    payload => "NewTopic");
```

If the payload object returns null the message will not be routed or published, but will simply be dropped. Otherwise the string returned will be used as the new topic to publish.

## Use Cases

* Use Pub/Sub to implement the Domain Events pattern in a system with multiple bounded subdomains. Using an [Outbox](Outbox.md) to only publish messages when domain data has been committed may be a good option.
* Use Pub/Sub to remove long Chain of Responsibility patterns or long sequences of `event`/`EventHandler<T>` where most handlers simply redirect to another handler, which is frequently done to push data back up a call stack. If you're using events to send a message up the call chain to a manager object and then using method calls to send the message down to where it's really needed, you should use Pub/Sub instead.
* Use Pub/Sub to send data to output streams such as files, sockets and logging systems. Acquaintance can automatically serialize these requests to a single thread if your communication mechanism isn't thread-safe.
* Use Pub/Sub along with the WorkerPool or .NET ThreadPool to distribute work to worker threads without having to implement and maintain your own work queues and thread management code.

## Examples

### Shared Log File

I have multiple worker threads who all want to log to a single log file. I only want to log events of a high severity. I can use a dedicated worker thread to make all requests to the shared log file happen on a single thread to avoid data corruption and deadlocks, and obviate the need for explicit locking or serialization:

```csharp
var token = messageBus.Subscribe<LogData>(builder => builder
    .WithTopic("Log")
    .Invoke(data => File.AppendAllText(fileName, data.ToString()))
    .OnDedicatedWorker()
    .WithFilter(data => data.Severity >= Severity.Error));
```

### Multiple Log Files

I have several shared log files, similar to the example above, but I want to send log events to different files depending on the source of the event: I would like all log events to use a single thread for writing:

```csharp
// Create a single thread to handle all logging
var workerToken = messageBus.ThreadPool.StartDedicatedWorker();

// Setup topics for each module
var tokenA = messageBus.Subscribe<LogData>(builder => builder
    .WithTopic("ModuleA")
    .Invoke(data => File.AppendAllText(fileNameA, data.ToString()))
    .OnThread(workerToken.ThreadId));
var tokenB = messageBus.Subscribe<LogData>(builder => builder
    .WithTopic("ModuleB")
    .Invoke(data => File.AppendAllText(fileNameB, data.ToString()))
    .OnThread(workerToken.ThreadId));

// Setup a routing rule to forward a log request to the appropriate
// topic
var routeToken = messageBus.SetupPublishRouting<LogData>("",
    builder => builder
        .When(d => d.Module == "A", "ModuleA")
        .When(d => d.Module == "B", "ModuleB"));

// Now publish a log message and it will go to the correct file
messageBus.Publish<LogData>(new LogData {
    Module = "A",
    ...
});
```

### Load-Balanced Web Service

I have two instances of a Web Service in my network, and I would like my application to automatically load balance my requests to all available servers:

```csharp
// Subscribe two topics, one for server A and one for server B
var serverAToken = messageBus.Subscribe<MyEvent>(builder => builder
    .WithTopic("ServerA")
    .Invoke(e => SendTo(serverAUrl, e)));
var serverBToken = messageBus.Subscribe<MyEvent>(builder => builder
    .WithTopic("ServerB")
    .Invoke(e => SendTo(serverBUrl, e)));

// Setup round-robin distribution to both topics
var routeToken = messagebus.SetupPublishDistribution<MyEvent>("",
    new[] {
        "ServerA",
        "ServerB"
    }
);

// Now publish an event, and it will automatically go to one of
// the two servers:
messageBus.Publish<MyEvent>(myEvent);
```