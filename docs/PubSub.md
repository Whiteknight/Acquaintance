## Publish/Subscribe

Publish/Subscribe ("Pub/Sub") is a messaging pattern where a component publishes a message on a **channel** and all **subscribers** to that channel receive a copy of the message. 

### Channels

Acquaintance Pub/SubChannels are defined by two pieces of information: A payload **type** and a **topic**. The default topic, if none is provided, is the empty string.

Subscribers subscribe to a particular channel and publishers publish messages to a particular channel. 

Acquaintance has two modes of operation. The first treats topic strings as literals and makes a single match to the channel with the given topic. The second mode allows topic strings to contain **wildcards** and all channels are selected which match the pattern.

#### Default Topic

The default topic is an empty string. Null strings are coalesced to the empty string. These three calls are all equivalent:

```csharp
messageBus.Publish<int>("", 1);
messageBus.Publish<int>(null, 1);
messageBus.Publish<int>(1);
```

#### Wildcards

Wildcard topic matching is more flexible but also incurs a slight performance penalty. To enable wildcards, you must specify the option when you create the message bus:

```csharp
var messageBus = new MessageBus(new MessageBusCreateParameters {
    AllowWildcards = true
});
```

With wildcards enabled, topic strings are parsed by separating on periods ('.') and matching parts with an asterisk ('*')

```csharp
// Publishes to topics like 'A.B.C' and 'A.B.X'
messageBus.Publish<MyMessage>("A.B.*", message);
```

Wildcard topics are only valid for publishing. You cannot subscribe with a topic containing a wildcard.

### Publishing

Acquaintance is optimized so that the common operations should be fast, while the uncommon operations do not need to be. Publishing is considered to be the most common operation in the Pub/Sub pattern and so more care has been taken to optimize that pathway than others.

Publishing, as seen in examples above, can be done with a single method call and only requires specifying a message type, a topic and a message payload.

#### Anonymous Publish

In some situations the type of the message payload will not be known at compile time. In these cases you can use an anonymous publish:

```csharp
messageBus.Publish("topic", message.GetType(), message);
```

This method calls into the normal publish methods using reflection and may incur a performance penalty.

#### Envelopes

Internally, Acquaintance wraps published messages in an `Envelope<T>` object. Envelopes contain the payload and topic information and also contain a unique ID and metadata about the message. You can create and publish envelopes manually:

```csharp
var envelope = messageBus.EnvelopeFactory
    .Create<MyMessage>("topic", message);
```

Most fields of the envelope are immutable, but it does contain a mechanism for attaching metadata to the envelope, which can be inspected later by the subscriber. Metadata is specified as a key/value pair with both key and value being strings:

```csharp
envelope.SetMetadata("key", "value")
```

Envelopes are passed between threads and the usual pattern for supporting this is the Immutable Object pattern. However metadata can and will change, so it requires explicit synchronization. If you use metadata, understand that there will be some overhead associated with it.

### Subscribing

Publishing is simple and straight-forward, but Subscribing is where the real complexity lies. A subscription is a Composite object which encapsulates a number of options and behaviors. The most simple subscription method looks like this:

```csharp
messageBus.Subscribe<MyMessage>("topic", subscription);
```

You can build your own subscription object, but these can get complicated. Acquaintance provides a builder object which allows you to create the subscription you need:

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
```

Next specify an action with one of these methods:

```csharp
    // Invoke an action on the payload
    .Invoke(payload => { })

    // Invoke an action on the envelope
    .InvokeEnvelope(envelope => { })

    // Invoke a method on a handler object
    .Invoke(handler)

    // Instantiate a service to handle the message;
    .ActivateAndInvoke(
        payload => new Service(), 
        (payload, service) => { ... })

    // Transform the message to a new type and publish on a new channel
    .TransformTo<MyOtherMessage>(
        payload => new MyOtherMessage(), 
        "newTopic")
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

    // Only handle a limited number of events
    .MaximumEvents(5)

    // Make changes to the subscription object
    .ModifySubscription(subscription => { })
```

The subscription builder uses segregated interfaces to help protect you for specifying conflicting options. At each step, only a few methods will be available to you to choose. Don't fight it. Setup things in order and you'll avoid whole classes of potential bugs.

#### Unsubscribing

Acquaintance uses the Disposable object pattern for unsubscribing. Every `.Subscribe` method variant returns an `IDisposable` token. Disposing this token removes the subscription from the channel:

```csharp
var token = messageBus.Subscribe<int>(b => { ... });
token.Dispose();
```

Disposing the subscription token removes the subscription from the channel and cleans up all related resources. If you specified the `.OnDedicatedWorker()` option, disposing the token will also stop and cleanup the worker thread.

#### Errors

Exceptions thrown from the subscriber are not passed back to the publisher thread. These exceptions are caught and logged internally to Acquaintance. If you want to see these errors, setup a logger when you create the message bus:

```csharp
var messageBus = new MessageBus(new MessageBusCreateParameters {
    Logger = ...
});
```

By default Acquaintance does not log anywhere, but you can easily create your own logger. It would be easy to adapt `Common.Logging`, `log4net` another logging tool to work with Acquaintance.

For simple purposes, you can just call a delegate with log messages to dump to the console, the debug window, or a file somewhere;

```csharp
var messageBus = new MessageBus(new MessageBusCreateParameters {
    Logger = new DelegateLogger(s => System.Console.WriteLine(s));
});
```

#### Autosubscribing

**This feature is currently experimental and is subject to change between versions**

If you don't need fine-tuned control over the options of your subscription, and would like them to be automatically created from attributes, you can use the autosubscription mechanism.

In your class annotate methods with the `SubscriptionAttribute`:

```csharp
[Subscription]
public void MyMethod(MyMessage payload) { ... }

[Subscription(Topics: new [] {"topic"})]
public void MyMethod(MyMessage payload) { ... }

[Subscription(Type: typeof(MyMessageType))]
public void MyMethod(MyMessageOrSubclass payload) { ... }

[Subscription]
public void MyMethod(Envelope<MyMessage> envelope) { ... }
```

The method currently must be public, must not be static, must return `void` and must take a single parameter of the correct type. If the scanner detects that any of these conditions are not satisfied, it will silently skip the method.

You cannot currently specify complicated options such as dispatch thread, maximum events, or anything else. 

#### Wrapping an Action

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

If you're passing actions around, this is a way to use Acquaintance internally without having to change your signatures.

### Routing 

Acquaintance allows you to setup routing rules on a topic, so that the message can be dispatched to other topics. The most straight-forward but least used method for this is `.AddRule()`:

```csharp
var token = messageBus.PublishRouter.AddRule("topic", rule);
```

There are other methods which simplify the creation of routing rules, which you should probably use instead.

As with all other places in Acquaintance, the `token` can be disposed to remove the rule from the router.

#### Predicate-Based Routing 

You can setup predicates to determine which message to dispatch to which topic: 

```csharp
var token = messageBus.SetupPublishRouting<MyMessage>("topic", b => b
    .When(payload => IsAMessage(payload), "TopicA")
    .When(payload => IsBMessage(payload), "TopicB")
    
    // Else clause is optional and can only be specified once
    .Else("TopicC"));
```

If routing is set up for a topic, none of the predicates match and there is no default, the publish will be ignored.

#### Distribution

Similar to routing, you can setup a round-robin distribution rule which will pick from a list of provided topics using a round-robin algorithm:

```csharp
var token = messageBus.SetupPublishDistribution("topic", new[] { 
    "TopicA", 
    "TopicB", 
    "TopicC" 
});
```

#### Route By Examination

Sometimes the payload object contains the information needed for its own routing. You can derive the topic to use by examining the payload object:

```csharp
var token = messageBus.SetupPublishByExamination("topic", 
    payload => "newTopic");
```

If the payload object returns null the message will not be routed or published. Otherwise the string returned will be used as the new topic to publish.

### Examples

#### Shared Log File

I have multiple worker threads who all want to log to a single log file. I only want to log events of a high severity. I can use a dedicated worker thread to make all requests to the shared log file happen on a single thread to avoid data corruption, locking or deadlocks:

```csharp
var token = messageBus.Subscribe<LogData>(builder => builder
    .WithTopic("Log")
    .Invoke(data => File.AppendAllText(fileName, data.ToString()))
    .OnDedicatedWorker()
    .WithFilter(data => data.Severity >= Severity.Error));
```

#### Multiple Log Files

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
    .Invoke(data => File.AppendAllText(fileNameA, data.ToString()))
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

#### Load-Balanced Web Service

I have two instances of a Web Service in my network, and I would like my application to automatically load balance my requests to all available servers:

```csharp
// Subscribe two topics, one for server A and one for server B
var serverAToken = messageBus.Subscribe<MyEvent>(builder => builder
    .WithTopic("ServerA")
    .Invoke(e => sendTo(serverAUrl, e)));
var serverBToken = messageBus.Subscribe<MyEvent>(builder => builder
    .WithTopic("ServerB")
    .Invoke(e => sendTo(serverBUrl, e)));

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