# Acquaintance

You can stay in touch without having to be all up in each others' business.

## Introduction

Acquaintance is a library for the internal messaging needs of a loosly-coupled application. Using a
messaging technology such as Acquaintance, individual components in your software can communicate
without having to maintain explicit references to each other or worry about order of initialization.

Acquaintance implements two messaging patterns: Publish/Subscribe and Request/Response. To start
using these, first create an `IMessageBus`:

    var messageBus = new MessageBus();

## Publish/Subscribe

Pub/Sub is a pattern where a publisher sends an event message payload to zero or more subscribers.
The following example will print the classic "Hello World" greeting to the console:

    // Create a subscription
    messageBus.Subscribe("test event", e => Console.WriteLine(e.Message))) 
    
    // Publish a message
    messageBus.Publish("test event", new MyEvent {
        Message = "Hello World"
    });
    
The type of payload object along with the name (`"test event"` in the above example) define a 
channel. A channel may contain any number of subscribers, and may be published to by any number of
publishers. 

The Pub/Sub pattern is very similar to the `event` / `EventHandler` functionality built-in to C#, but
unlike `event` you don't need an explicit reference to the publisher in order to subscribe to it.
Using Acquaintance, you can subscribe to a channel where there are no publishers at all, and create
those publishers later as needed.

## Request/Response

Request/Response is a pattern where a client sends a request to zero or more servers, and receives 
back zero or more responses in return. This example prints the classic "Hello World" greeting to the
console:

    // Create a subscription
    messageBus.Subscribe<MyRequest, MyResponse>("test", req => new MyResponse { 
        Message = "Hello " + req.Message"
    });
    
    // Send a request
    var response = messageBus.Request<MyRequest, MyResponse>("test", new MyRequest {
        Message = "World"
    });
    foreach (var message in response.Responses)
        Console.WriteLine(message);

The types of Request, Response and the name (`"test"` in the example above) define the req/res
channel. A channel may contain any number of subscribers. Any number of clients may make requests
on the channel.

## Managing Subscriptions

Every `Subscribe` method variant returns an `IDisposable`. This is a **subscription token** and
can be used to cancel the subscription. 

    // Create a subscription
    var subscription = messageBus.Subscribe("test event", e => Console.WriteLine(e.Message))) 
    
    // Remove the subscription
    subscription.Dispose();
    
    // Publish a message, but there are no subscribers! Nothing will happen.
    messageBus.Publish("test event", new MyEvent {
        Message = "Hello World"
    });

If you have multiple subscriptions to manage, you can use a `SubscriptionCollection` to keep track
of them.

    var subscriptions = new SubscriptionCollection(messageBus)
    
    // Create a subscription
    subscriptions.Subscribe("test event", e => Console.WriteLine(e.Message))) 
    
    // Disposes all subscriptions!
    subscriptions.Dispose();
    
## Thread Safety

Thread safety is handled by the subscriber. Every `Subscribe` method call takes an optional 
`SubscribeOptions` parameter, which can be used to control how the message is received.

    var subscription = messageBus.Subscribe("", ..., new SubscribeOptions {
        ...
    });
    
There are several options for scheduling delivery of an event or a request. If you don't care, you
can either ignore the `options` parameter entirely, pass `null`, or use:

    // The message will be handled wherever makes the most sense
    new SubscribeOptions {
        DispatchType = DispatchThreadType.NoPreference
    };

### Immediate Delivery

Using immediate delivery, the message will be handled on the thread of the client. This is a
blocking operation for the publisher or client.
    
    new SubscribeOptions {
        DispatchType = DispatchThreadType.Immediate
    };
    
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
    
### Specific Thread

The other type of thread that the message bus can have is a "Dedicated" worker. These are threads
which are created on demand and are addressed by ID. Dedicated workers are useful in situations
where the subscriber has resources which are not thread-safe, but need to process requests or events
from multiple sender threads. Instead of setting up an expensive lock mechanism, we can use a single
dedicated worker thread to handle these requests. 

    // First, ask the IMessageBus to start a dedicated worker thread:
    int threadId = messageBus.StartDedicatedWorkerThread();
    
    // The message will be handled only on the specific thread
    var options = new SubscribeOptions {
        DispatchType = DispatchThreadType.SpecificThread,
        ThreadId = threadId
    });
    
Notice that the `threadId` here is the same as 
`System.Threading.Thread.ManagedThreadId`. Technically speaking, you can send a
message to any thread whose ID you know. Acquaintance will helpfully queue the message against any
thread ID you give it, and will wait for that thread to check for messages. MessageBus worker 
threads listen in an event loop, threads you create elsewhere do not. If you want to manually handle
events on some other thread which the message bus does not control, you can do that.

First, you can use the `EmptyActionQueue` method to handle a number of messages on the current
thread, as a blocking operation:

    // Handle at most 10 queued messages on the current thread. 
    messageBus.EmptyActionQueue(10);
    
You can use this method to integrate Acquaintance into some other message processing loop.

Second, you can create a runloop on the current thread, and process messages on the current thread
as they arrive.

    messageBus.RunEventLoop();
    
`RunEventLoop` has some optional arguments which can be used to exit the runloop when it's time to
do something else.
    
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

    // SubscribeOptions to timeout after 5 seconds
    var options = new SubscribeOptions {
        TimeoutMs = 5000
    });
    
Keep in mind that Request/Response is difficult to parallelize and requires synchronization to make
sure that the responses make it back to the requesting thread correctly. Acquaintance does this
without locks, but does use other synchronization primitives to wait for a response. In order to
help keep yourself out of trouble, remember these rules:

1. The request object is the property of the request thread. The subscriber should not modify it. 
    using immutable objects is considered best practice for request objects.
2. When a subscriber returns a response, that response becomes the property of the request thread.
   The subscriber thread should never attempt to modify the response object after returning it.
3. Large numbers of subscribers on a Request/Response channel, even if they are threaded, are going
   to have a negative impact on performance. 
  
If large numbers of components need to be participating in request/response interactions or if
your developers aren't disciplined enough to follow rules about thread safety, you should consider
some other solution to meet your needs. 

Also, because the requesting thread blocks until all responses are received, there are plenty of
opportunities for deadlocking. Acquaintance gives you plenty of rope, but can only suggest you don't
hang yourself with it. Best practices include:

1. Use `TimeoutMs` on your subscribers if you think they will not respond in time. 
2. If you need lots of request/response and are sensitive to timing, use something else like a
    Mediator pattern instead
    
## Status

Acquaintance is currently in an experimental state. The core functionality seems to be working as
expected but it has not been through sufficient testing for real-world use. There are also several
important features which have not yet been implemented.