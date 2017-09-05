## Sagas Module

**Sagas are an experimental feature and will change in future versions**

Sagas perform event aggregation for pub/sub messages. A saga is started by receiving a specific type of message and is continued by receiving zero or more additional follow-up messages. When all messages in the saga have been received a final completion action will be executed.

Because state needs to be maintained between received messages, the Sagas module needs to maintain data storage. It is the only module with data storage requirements, and for that reason the module should be used with care and old/incomplete sagas should be cleaned up regularly.

First, start by initializing the Sagas module. Specify the number of threads to use with the sagas module:

```csharp
var token = messageBus.InitializeSagas(numberOfThreads);
```

### Create a Saga

```csharp
var token = messageBus.CreateSaga<MyState, MyKey>(builder => builder)
    // First setup a message to start the saga with
    .StartWith<MyMessage1>("topic", payload => key, payload => state, context => { ... })

    // Setup any additional messages which will be part of the saga
    // These can be configured in any order and can be received by the saga
    // in any order
    .ContinueWith<MyMessage2>("topic", payload => key, context => { ... })

    // Finally specify an action to execute when the saga is complete
    .WhenComplete((bus, state) => { ... })
);
```