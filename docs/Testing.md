## Testing Module

**The Testing module is experimental and changes to the API will be made in future versions**

Acquaintance provides a module with unit testing tools to help in unit testing code which uses messaging patterns. The testing module works very similar to the Mock Object pattern: Expectations are setup before the test and are verified after the test has completed. 

First you must initialize the Testing module:

```csharp
var token = messageBus.InitializeTesting();
```

When the testing module is initialized, you set up your expectations, run your code, and then verify that all your expectations have been met:

```csharp
// Default behavior, throw a generic Exception when an expectation
// is missed
messageBus.VerifyAllExpectations();

// Specify custom behavior when an expectation is missed
messageBus.VerifyAllExpectations(onError);
```

### Pub/Sub Testing

```csharp
// Create the expectation
var expectation = messageBus.ExpectPublish<MyEvent>("topic", filterPredicate);

// Optionally execute a callback when the expectation is met
expectation.Callback(payload => { ... });
```

### Request/Response Testing

```csharp
// Create the expectation
var expectation = messageBus.ExpectRequest<MyRequest, MyResponse>("topic", filterPredicate);

// Specify what response to return
expectation.WillReturn(response);
expectation.WillReturn(request => response);

// Optionally execute a callback when the expectation is met
expectation.Callback(request => { ... });
```

### Scatter/Gather Testing

```csharp
// Create the expectation
var expectation = messageBus.ExpectScatter<MyRequest, MyResponse>("topic", filterPredicate);

// Specify what response to return
expectation.WillReturn(response);
expectation.WillReturn(request => response);

// Optionally execute a callback when the expectation is met
expectation.Callback(request => { ... });
```