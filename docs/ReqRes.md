## Request/Response

**Request/Response** ("Req/Res") is another messaging pattern that might be more familiar to most programmers. It works like an indirect method call, an RPC method call, or something like an HTTP webservice call where there is a request and a response. A channel may have a single **Listener**, which responds to incoming requests. 

### Channels

Req/Res channels are defined by three pieces of information: A **request type**, a **response type** and an optional **topic**. The default topic, if none is provided, is the empty string. Null topics are coalesced to the empty string.

Every Req/Res channel may have a single Listener. Attempting to add a second listener to the channel will throw an exception.

Acquaintance has two different ways to treat topics: First as literal strings and second parsed to allow wildcard behavior.

#### Wildcards

Wildcard topic matching is more flexible but also incurs a slight performance penalty. To enable wildcards, you must specify the option when you create the message bus:

```csharp
var messageBus = new MessageBus(new MessageBusCreateParameters {
    AllowWildcards = true
});
```

With wildcards enabled, topic strings are parsed by separating on periods ('.') and matching parts with an asterisk ('*')

```csharp
// Sends the Request to topics like 'A.B.C' and 'A.B.X'
var response = messageBus.Request<MyRequest, MyResponse>(
    "A.B.*", request);
```

If a wildcard matches more than one channel, only a single Listener will be selected. Which one is selected should not be treated as deterministic or reliable.

Wildcard topics are only valid for Requests. You cannot add a Listener with a topic containing a wildcard.

### Requests

Requests are inherently an asynchronous operation, and a response may not be available immediately. There are a few different ways to make a request and wait for the response, depending on your needs.

Unlike Pub/Sub where errors are swallowed by the dispatcher and logged, Exceptions from a Listener are communicated back to the Requester. How errors are handled is determined by how the Request is made.

If a request is made on a channel with no Listener configured, the response will be a default value for that type and will be returned immediately. Depending on the method used to make the request, it may be possible to determine if there was a Listener or not.

#### RequestWait

The most simple way to make a request is to specify the channel, pass a request object, and call `.RequestWait()`:

```csharp
// Use default topic
var response = messageBus.RequestWait<MyRequest, MyResponse>(request);

// Specify topic explicitly
var response = messageBus.RequestWait<MyRequest, MyResponse>(
    "topic", request);

// Specify timeout explicitly
var response = messageBus.RequestWait<MyRequest, MyResponse>(
    "topic", request, timeout);
```

`.RequestWait()` waits for the response up to a default timeout (10 Seconds). If there is an exception, it is thrown. Otherwise, the response payload (or a default value) is returned. If the request times out, the default value is returned.

#### RequestAsync

`.RequestAsync` is a simple request mechanism like `.RequestWait()` but it uses `System.Threading.Task<TResponse>`.  Use this if your system is making use of `async`/`await` and wants to chain Tasks together:

```csharp
// With the default topic
var task = messageBus.RequestAsync<MyRequest, MyResponse>(request);

// Specify topic explicitly
var task = messageBus.RequestAsync<MyRequest, MyResponse>(
    "topic", request);

task.Wait();
var response = task.Result;
```

The task will communicate exceptions and completion information using the normal interface.

#### Request

To get the most control over the request, use the `.Request()` method:

```csharp
// make the request using the default topic
var request = messageBus.Request<MyRequest, MyResponse>(request);

// Specify the topic explicitly
var request = messageBus.Request<MyRequest, MyResponse>(
    "topic", request);

// Wait for the response using a default timeout or specify one 
// explicitly
request.WaitForResponse();
request.WaitForResponse(timeout);

// Determine if the request completed, or timed out
bool isComplete = request.IsComplete();

// Determine if there was a Listener or no Listener on this channel
bool hasResponse = request.HasResponse();

// Get an exception if present or null
Exception e = request.GetErrorInformation();

// Throw the exception if present, or do nothing
request.ThrowExceptionIfError();

// Get the response
var response = request.GetResponse();
```

#### Anonymous Requests

#### Request Envelopes

### Listeners

Making a request is relatively easy. Listeners contain most of the complexity and setting up a Listener is much more involved. A Listener is a Composite Object which encapsulates a number of options and behaviors.

The most straight-forward but least common way to add a Listener to a channel is like this:

```csharp
var token = messageBus.Listen<MyRequest, MyResponse>(
    "topic", listener);
```

Creating a listener can be difficult, so a Builder object is provided to simplify. First, set your topic:

```csharp
var token = messageBus.Listen<MyRequest, MyResponse>(
    builder => builder
        // With the default topic
        .WithDefaultTopic()

        // Specify the topic explicitly
        .WithTopic("topic")

        ...
);
```

Next, specify what you want to happen when the Request is received:

```csharp
    // Invoke a Func on the request payload
    .Invoke(request => new MyResponse())

    // Invoke a Func on the raw envelope
    .InvokeEnvelope(envelope => new MyResponse())

    // Create a service to handle the request
    .ActivateAndInvoke(request => new MyService(), 
        (request, service) => service.GetResponse(request))

    // Transform the request to a new type, and dispatch on a new channel
    .TransformRequestTo<MyRequest2>("newTopic", 
        request => new MyRequest2())

    // Redirect to a different channel, and transform the response
    .TransformResponseFrom<MyResponse2>("newTopic", 
        originalResponse => new MyResponse())
```

Optionally you can specify the way to dispatch the request on a thread:


```csharp
    // On an Acquaintance worker thread (Default)
    .OnWorker()

    // Immediately on the publisher thread (not recommended)
    .Immediate()

    // On a specific .NET thread
    .OnThread(threadId)

    // On the .NET Threadpool (using System.Threading.Task)
    .OnThreadPool()

    // Create a new worker thread, and use only that thread for this 
    // listener
    .OnDedicatedWorker()
```

Finally you can specify any additional details as necessary:

```csharp
    // Only handle a specific number of requests:
    .MaximumRequests(5)

    // Only handle requests which match a predicate
    .WithFilter(request => true)

    // Use a CircuitBreaker pattern to handle errors
    .WithCircuitBreaker(numberOfErrors, timeoutMs)

    // Modify the Listener 
    .ModifyListener(listener => ...)
```

The Listener Builder uses segregated interfaces to only provide certain methods at certain times to avoid conflicting settings. Don't fight it! If you don't see a method you want, keep configuring until you do see the correct methods.

#### Stop Listening

The `.Listen()` method and all it's variants return a **Listener Token**. Disposing this token will remove the listener from the channel and cleanup all relevant resources.

```csharp
var token = messageBus.Listen<int, string>(builder => ...);
token.Dispose();
```

#### Wrapping a Function

#### Circuit Breaker Pattern

The Circuit Breaker Pattern disconnects a resource when a certain number of consecutive errors has been reached, to prevent flooding. The resource will remain disconnected for a certain timeout, in hopes that normal operation can be restored.

### Examples

#### Fragile Web Service

I have to make a call to a fragile web service. The service may occasionally crash, requiring 10 seconds to reboot. This service returns a string of JSON, which we want to parse into a proper response object

```csharp
var token1 = messageBus.Listen<MyRequest, string>(builder => builder
    .WithTopic("Raw")
    .Invoke(request => webService.Request(request))
    .OnWorker()
    .WithCircuitBreaker(5, 10000));
var token2 = messageBus.Listen<MyRequest, MyResponse>(
    builder => builder
        .WithTopic("Parsed")
        .TransformResponseFrom<string>("Raw", 
            json => ParseJson<MyResponse>(json)));
```

Now I can make the request and get the required response:

```csharp
var response = messageBus.RequestWait<MyRequest, MyResponse>(
    "Parsed", request);
if (response == null) {
    // The circuit breaker is probably tripped
}
```

