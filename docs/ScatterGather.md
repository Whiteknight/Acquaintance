# Scatter/Gather

The third major pattern supported by Acquaintance is **Scatter/Gather**. Scatter/Gather is like a combination of Pub/Sub and Request/Response patterns: Many listeners or "Participants" can be attached to a channel and each of them is able to reply to a request.

Scatter/Gather is significantly more complex than either Pub/Sub or Request/Response, and there are more gotchas and pitfalls to be aware of.

## Use-Cases

Scatter/Gather is useful for a few broad use-cases:

1. Bidding. Many components bid to produce a "best" answer, and the caller selects the best one from the list.
1. Map/Reduce. Many components each return a partial answer, and these partial answers are combined together to form a single complete answer.
1. Pub/Sub with Receipt. For cases where it's not good enough to simply publish a message and forget it. Scatter/Gather allows you to receive confirmation that all recipients received and acted upon the event information.

## Channels

Like Request/Response, Scatter/Gather channels are defined by three pieces of information: The **Request Type**, the **Response Type** and a string **Topic**. The default topic is the empty string. Null topics are coalesced to the empty string.

Scatter/Gather does Wildcard topic matching similar to Request/Response.

## Requests

Scatter/Gather requests are more complicated than Request/Response requests, because there are potentially many participants on the channel and each of them may return responses at different times. Scatter/Gather requests are made using the `.Scatter()` method.

```csharp
var scatter = messageBus.Scatter<MyRequest, MyResponse>(
    "topic", request);

// Get the total number of participants (available immediately)
var participants = scatter.TotalParticipants;

// Wait for the next response using a default timeout (10 seconds)
// or an explicit timeout
var response = scatter.GetNextResponse();
var response = scatter.GetNextResponse(timeout);

// Determine if the response was successful or threw an Exception
bool ok = response.Success
var payload = response.Value;
Exception error = response.ErrorInformation;
response.ThrowExceptionIfPresent();

// Start a System.Threading.Task to get the next response
var task = scatter.GetNextResponseAsync(timeout, cancellationToken);
task.Wait();
var payload = task.Result;

// Wait for several responses, using an optional timeout and/or a
// maximum number:
var responses = scatter.GatherResponses();
var responses = scatter.GatherResponses(maxResponses);
var responses = scatter.GatherResponses(timeout);
var responses = scatter.GatherResponses(maxResponses, timeout);

// Get the total number of received responses
int numberOfResponses = scatter.CompletedParticipants;
```

Due to the nature of multi-threaded computing, keep in mind the following scenario:

```csharp
var responses = scatter.GatherResponses(maxResponses);
if (responses.Count > scatter.CompletedParticipants) {
    ...
}
```

The call to `scatter.GatherResponses()` may return with responses before the value `scatter.CompletedParticipants` has had time to update. You may be able to read more responses than the system thinks are available. `scatter.CompletedParticiants` should be used more as a guideline, keeping in mind the nature of multi-threaded logic.

## Participants

Setting up a Participant for Scatter/Gather is similar in complexity to setting up a Listener for Request/Response. Many of the same patterns and methods are available for both.

The most straight-forward but least common way to add a Participant to a channel is like this:

```csharp
var token = messageBus.Participate<MyRequest, MyResponse>(
    "topic", participant);
```

Creating a participant can be difficult, so a Builder object is provided to simplify. First, set your topic:

```csharp
var token = messageBus.Participate<MyRequest, MyResponse>(
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
    // participant
    .OnDedicatedWorker()
```

Keep in mind that if you have many participants scheduled on a small number of worker threads, you will need to wait for some to complete before others can get started. Try to keep in mind the size of your scatters when you're decided how many threads to allocate for the WorkerPool or whether to use the .NET ThreadPool.

Finally you can specify any additional details as necessary:

```csharp
    // Only handle a specific number of requests:
    .MaximumRequests(5)

    // Only handle requests which match a predicate
    .WithFilter(request => true)

    // Use a CircuitBreaker pattern to handle errors
    .WithCircuitBreaker(numberOfErrors, timeoutMs)

    // Modify the Participant
    .ModifyParticipant(participant => ...)
```

The Participant Builder uses segregated interfaces to only provide certain methods at certain times to avoid conflicting settings. Don't fight it! If you don't see a method you want, keep configuring until you do see the correct methods.

### Stop Participating

The `.Participate()` method and all it's variants return a **Participant Token**. Disposing this token will remove the participant from the channel and cleanup all relevant resources.

```csharp
var token = messageBus.Participate<int, string>(builder => ...);
token.Dispose();
```

### Circuit Breaker Pattern

## Use Cases

* Use Scatter/Gather as a result aggregator, receiving responses from multiple data sources and combine those together into a single piece of complete data
* Use Scatter/Gather for a bidding system, sending requests to multiple modules and using only the best response
* Use Scatter/Gather to implement Map/Reduce, sending a problem out to multiple modules, and then combining partial data sets into a single complete data set.
* Use Scatter/Gather to monitor health and status for multiple parts of your system
* Use Scatter/Gather similarly to Pub/Sub with a read receipt. Make sure all parts of your system received your message and processed it successfully.

## Examples

### Map/Reduce

My system connects to three databases, each of which contains a subset of user event data. One holds security events such as password changes, one holds billing events, and one holds non-security profile change events. The user would like to see an ordered list of all events on their account.

```csharp
var scatter = messageBus.Scatter<UserEventRequest, List<UserEvent>>(new UserEventRequest(userId));

// Get the events from the modules which successfully reply
var events = scatter.GatherResults()
    .Where(r => r.IsSuccess && !r.IsEmpty)
    .SelectMany(r => r.Value);

// Order those events by date, descending, and return a list
return events
    .OrderByDescending(e => e.EventDate)
    .ToList();
```

Each module may define a participant like this, the specifics will depend on which data store or ORM you are using:

```csharp
messageBus.Participate<UserEventRequest, List<UserEvent>>(b => b
    .WithDefaultTopic()
    .Invoke(r => dataSource
        .Query<UserEvent>()
        .Where(ue => ue.UserId == r.UserId)
        .ToList()));
```
