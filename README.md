# Acquaintance

You can stay in touch without having to be all up in each others' business.

## Project
### Status

Acquaintance 1.0.1 is released and ready for most use.
Acquaintance 2.0.0 is under active development with many improvements and breaking changes.

### Contributing

Feedback, bug reports and feature requests are accepted and appreciated. 

## Introduction

Acquaintance is a library for the internal messaging needs of a loosly-coupled application. Using a
messaging technology such as Acquaintance, individual components in your software can communicate
without having to maintain explicit references to each other or worry about order of 
initialization.

Acquaintance implements several messaging patterns: Publish/Subscribe, Request/Response and
Scatter/Gather. To start using these, first create an `IMessageBus`:

    var messageBus = new MessageBus();

## Use Cases

Acquaintance can act like the messaging "glue" for a loosely-coupled application or it can serve as
an intermediate step when trying to migrate a monolithic application to a distributed,
service-oriented application. Acquaintance can also be used to help alleviate concurrency problems
with non-thread-safe resources, or distributing tasks to worker threads.

## Publish/Subscribe

Pub/Sub is a pattern where a publisher sends an event message payload to zero or more subscribers.
It is used when you need to alert many parts of your application about an event or occurance, but
a response to those events is not required.

    // Create a subscription
    messageBus.Subscribe<MyEvent>(s => s
        .WithTopic("test event")
        .Invoke(e => Console.WriteLine(e.Message)));
    
    // Publish a message
    messageBus.Publish("test event", new MyEvent {
        Message = "Hello World"
    });
    
The type of payload object along with the topic (`"test event"` in the above example) define a 
**channel**. A channel may contain zero or more subscribers, and may be published to by any number of
publishers. 

## Request/Response

Request/Response is a pattern where a client sends a request to zero or one listeners, and receives 
back a single response in return. It is used in cases where you need to serialize access to a
single resource which is not thread-safe (DB, File System, Socket, etc), or when you need to interact
with a separate subsystem or module in your system.

    // Setup a Listener
    messageBus.Listen<MyRequest, MyResponse>(l => l
        .WithTopic("test")
        .Invoke(req => new MyResponse { 
            Message = "Hello " + req.Message"
        }));
    
    // Send a request
    var response = messageBus.Request<MyRequest, MyResponse>("test", new MyRequest {
        Message = "World"
    });
    Console.WriteLine(response.Message);

The types of Request, Response and the topic (`"test"` in the example above) define the 
request/response channel. 

Acquaintance allows a Request/Response channel to only have zero or one Listener. Any attempt to 
add an additional Listener to an existing channel will throw an Exception. Every Request will
generate either one response (which may be an exception, if the listener threw one) or a default value.

## Scatter/Gather

Scatter/Gather is a pattern very similar to Request/Response except it allows multiple listeners
to participate in the conversation and the client will return many responses. 

    // Setup a Listener
    messageBus.Participate<MyRequest, MyResponse>(p => p
        .WithTopic("test")
        .Invoke(req => new MyResponse { 
            Message = "Hello " + req.Message"
        }));
    
    // Send a request
    var scatter = messageBus.Scatter<MyRequest, MyResponse>("test", new MyRequest {
        Message = "World"
    });
    var responses = scatter.GatherResponses();
    foreach (var message in response.Response)
        Console.WriteLine(response.Message);

Scatter/Gather can be used in places where you want to compare multiple results together:

1. You need to gather several bids or opinions so you can select the best one
2. You need to read values from multiple sensors to calculate an average or aggregation
3. You need to read data from several storage locations to assemble a single aggregate object
4. You want to publish an event to multiple recipients, and receive back confirmation that they are received

## Managing Subscriptions

Every `Subscribe`, `Listen`,  and `Participate` method variant returns an 
`IDisposable` **subscription token**. This token can be disposed to cancel the subscription:

    // Create a subscription
    var subscription = messageBus.Subscribe(...) 
    
    // Remove the subscription
    subscription.Dispose();

If you have multiple subscriptions to manage, you can use a `SubscriptionCollection` to keep
track of them. `SubscriptionCollection` acts like a wrapper around the message bus, and holds
the generated tokens so they can all be disposed of at once. 

    var subscriptions = new SubscriptionCollection(messageBus)
    
    // Create subscriptions
    subscriptions.Subscribe("test event", e => Console.WriteLine(e.Message));
    ...
    
    // Disposes all subscriptions!
    subscriptions.Dispose();

This is useful when you have multiple modules in your application and a module maintains several of
its own subscriptions, which all need to be disposed when the module is disposed.

Acquaintance uses the `IDisposable` pattern to cleanup and manage several different types of
resources.
    
## Thread Safety

Thread safety is handled by the subscriber, listener or participant. When building a subscription,
listener or participant, there are options available to control how the message is received:

    var token = messageBus.Subscribe(b => b
        .WithTopic("...")
        .Invoke(...)
        .Immediate()    // Run on the current thread
    });
    
There are several options for scheduling delivery of an event or a request. 

1. `.Immediate()` executes the handler immediately in the current thread, which means the operation will block
2. `.OnWorkerThread()` executes the handler on one of the managed Acquaintance worker threads
3. `.OnThreadpool()` executes the handler in the .Net threadpool
4. `.OnThread(int)` dispatches the handler to the thread with the given thread Id
5. `.OnDedicated()` creates a new dedicated worker thread for this subscriber and sends all requests there

If no option is specified, Acquaintance will dispatch the handler in a way that makes the most sense.

### Immediate Delivery

Using immediate delivery, the message will be handled on the thread of the client. This is a
blocking operation for the publisher or client. This can be bad for several reasons:

1. The operation will block until the handler has completed. 
2. It causes recursion on the call stack, which can grow large if there are many events to call in a row

At the same time, this is the only option which is time-deterministic, so it is an excellent option
for unit testing where you don't want all your tests to use a `ManualResetEvent` or similar tool
to wait for results (and then get angry at the false-positives when your system is under load and
your tests take longer than your timeout).
    
### Random Worker Thread

The message bus can maintain a pool of worker threads. There are two types of worker threads, 
"Free" workers and "Dedicated" workers. Free workers don't have IDs, so they handle messages in a
round-robin scheme. If you don't care where the message is handled, and don't want the sender to be
blocked, dispatching the message to a worker thread is a good choice.

    // First, tell the IMessageBus to start 4 worker threads:
    messageBus.StartWorkers(4);
    
    // The message will be handled on any random Free worker thread 
    var options = new SubscribeOptions {
        DispatchType = DispatchThreadType.AnyWorkerThread
    };

Notice that if you specify `.OnWorkerThread()` but your message bus doesn't have any worker
threads available, it will use the .Net thread pool instead.
    
### Specific Thread

The other type of thread that the message bus can have is a "Dedicated" worker. These are threads
which are created on demand and are addressed by ID. Dedicated workers are useful in situations
where the subscriber or listener has resources which are not thread-safe, but need to process 
requests or events from multiple sender threads. Instead of setting up an expensive lock mechanism, 
we can use a single dedicated worker thread to handle these requests. 

    // First, ask the IMessageBus to start a dedicated worker thread:
    int threadId = messageBus.ThreadPool.StartDedicatedWorkerThread();
    
    // The message will be handled only on the specific thread
    messageBus.Subscribe<MyEvent>(b => b
        ...
        .OnThread(threadId));
    
Notice that the `threadId` here is the same as `System.Threading.Thread.ManagedThreadId`. 
Technically speaking, you can send a message to any thread whose ID you know. Acquaintance will 
helpfully queue the message against any thread ID you give it, and will wait for that thread to 
check for messages. MessageBus worker threads listen in an event loop, threads you create elsewhere
do not. If you want to manually handle events on some other thread which the message bus does not 
control, you can do that in one of two ways:

First, you can use the `EmptyActionQueue` method to handle a number of messages on the current
thread, as a blocking operation:

    // Handle at most 10 queued messages on the current thread. 
    messageBus.EmptyActionQueue(10);
    
You can use this method to integrate Acquaintance into some other message processing loop.

Second, you can create a runloop on the current thread, and process messages on the current thread
as they arrive.

    messageBus.RunEventLoop();
    
`RunEventLoop` has some optional arguments which can be used to exit the runloop when it's time 
to do something else. Otherwise it will run forever until the program is terminated.
    
### Thread-Safe Pub/Sub

Pub/Sub events are fire-and-forget for the publishing thread, and typically only creates an effect
on the subscribers. There are several rules to remember when using Pub/Sub, if you want to maintain
thread safety:

1. Subscribers can be invoked at any time. The publisher should not modify the payload object after
    publishing it to avoid thread-safety issues. 
2. Subscribers should not modify the payload object, because they don't know how many other
    subscribers there are, or how many threads are handling this same message at the same time.
3. Use event names for things like message enrichment. A name like "first" can be used to get a raw
    message and enrich it, and then a name like "second" can be used to get the enriched object.
    In this way, you can avoid timing issues where one thread is enriching a message while another
    thread is looking for data in null properties.
    
Immutable event objects are considered best-practice for publishing to solve several thread-safety
problems. 

### Thread-Safe Request/Response 

Request/Response messages can be handled on separate threads as well. The request will block until
all responses are received, though subscribers may define a timeout if you are worried about a
hang.

    // Timeout after 5 seconds
    messageBus.Listen<MyRequest, MyResponse>(l => l
        ...
        .WithTimeout(5000));
    
Keep in mind that Request/Response is difficult to parallelize and requires synchronization to make
sure that the responses make it back to the requesting thread correctly. Acquaintance does this
without locks, but does use other synchronization primitives to wait for a response. In order to
help keep yourself out of trouble, remember these rules:

1. The request object is the property of the request thread. The subscriber should not modify it. 
    Using immutable objects is considered best practice for request objects.
2. When a listener returns a response, that response becomes the property of the request thread.
   The listener thread should never attempt to modify the response object after returning it.
   Using immutable objects is considered best practice for response objects.
3. Chains of request/response (one listener makes a request on another listener, etc) are going to
    have a negative impact on performance.

Also, because the requesting thread blocks until all responses are received, there are plenty of
opportunities for deadlocking. Acquaintance gives you enough rope to hang yourself with, but by
following some best-practices you should be alright:

1. Use `.WithTimeout()` on your subscribers if you think they will not respond in time or if
    there is any potential for deadlocks. The system has a default timeout value, do not modify
    or remove this protection without serious consideration for side-effects.
3. If you need lots of request/response and are sensitive to timing, use something else like a
    Mediator pattern instead. Or, use a pub/sub channel to send an event to start a long-running
    process, and use another pub/sub channel to receive responses when they are ready. This way
    your sender doesn't block for long periods of time.
    
## Routing

Routing works by sending an event or request to a particular channel name, and the router will
redirect to a new channel name. In this way, you have have different recipients on different 
channel names, and route to them based on routing rules.

    messageBus.Subscribe<MyEvent>(b => b
        ...
        .Distribute(new [] { "a", "b", "c" }));

`.Distribute()` uses a round-robin algorithm to pass messages to the channel names provided. The
first message will go to channel "a", the second to channel "b", the third to "c", and then the
fourth message will go to "a" again. This feature acts like a simple load-balancer, and is mostly
used to balance access to external services.

    messageBus.Subscribe<int>(b => b
        ...
        .Route(r => r
            .When(i => i < 10, "a")
            .When(i => i > 10 && i < 100, "b")
            .When(i => i > 100, "c")));

The `.Route()` method allows to you provide a list of predicates, and the channels to route to
if a predicate is satisfied. In this example, any `int` event published to the default channel
name (`null` or `string.Empty`) will be routed to channel "a" if the integer is less than 10,
to channel "b" if the integer is between 10 and 100, and to channel "c" if the integer is greater
than 100. All other integer values will result in the message not being routed.

## Nets

Nets are networks for computation which use an IMessageBus internally. You can create a Net and
define the processing steps for incoming data. Then when the data arrives, the Net will invoke the
necessary steps, in order, and making full use of parallelism.

Nets are useful when you have a number of loosely-coupled processing steps, but don't want to
hard-code an execution order, or hard-code priority values for each step (and then have to change
all priority values when a new one needs to be inserted, etc). 

Think about Nets as being a small, in-process version of something like *Apache Storm*.

