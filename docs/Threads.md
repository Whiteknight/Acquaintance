## Threading

Acquaintance keeps track of several different types of threads for dispatching various messages and requests. In addition to the .NET ThreadPool and managed threads, Acquaintance maintains its own pool of worker threads. Worker threads come in two varieties: **Free Workers** which can process any requests which come in and **Dedicated Workers** which only handle requests and messages from specific handlers.

### Free Workers

Free Worker threads are created when the MessageBus is initialized. The default number of free worker threads is 2, but this value is configurable:

```csharp
var messageBus = new MessageBus(new MessageBusCreateParameters {
    NumberOfWorkers = 4
});
```

Setting the number of workers to 0 will cause Acquaintance to not allocate any worker threads.

Handlers using the `.OnWorker()` method or not specifying a dispatch will use Free Worker threads by default, unless Acquaintance is configured to not use any. In that case these events will go to the .NET ThreadPool instead.

### Dedicated Workers

Dedicated Worker threads can be created by using the `.OnDedicatedWorker()` method when creating a Subscriber/Listener/Participant or by explicitly creating one:

```csharp
WorkerToken worker = messageBus.WorkerPool.StartDedicatedWorker();

// Determine if the thread was created successfully
bool ok = worker.IsSuccess;

// Get the ThreadId
int threadId = worker.ThreadId;

// Stop the thread and free all usages
worker.Dispose();
```

Once you have the worker thread token, You can dispatch events to that thread using the `.OnThread()` method. This can be useful in cases where several related events and requests can all be serialized to the same thread to avoid conflicts, data corruption and deadlocks.

```csharp
messageBus.Subscribe<int>(b => b
    .WithDefaultTopic()
    .Invoke(p => ...)
    .OnThread(worker.ThreadId)));
```

### Detached Contexts and Runloops

Acquaintance will actually allow you to dispatch an event or request to any thread by Id, including threads which were not created as part of the worker pool. Other threads can manually poll for incoming events. This is useful for cases where you need to integrate Acquaintance actions into an existing runloop, or synchronize requests to an existing thread to avoid data contention:

```csharp
messageBus.EmptyActionQueue(numEvents);
```

You can also start a runloop of your own on any thread, polling in a loop without returning:

```csharp
messageBus.RunEventLoop();
```

The runloop does allow a callback to exit the loop:

```csharp
messageBus.RunEventLoop(() => shouldStop);
```

### Managed Threads

