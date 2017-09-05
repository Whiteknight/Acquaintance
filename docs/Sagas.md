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
var token = messageBus.CreateSaga<MyState, MyKey>(builder => builder
    ...
);
```

The Saga builder has a few methods which you can call to setup the Saga. First, you must specify a start message. The start message and every message thereafter must somehow provide a unique **Key**. The Key is used to link messages together, and look up the stored state object of the ongoing saga. The start message must provide this key and must also provide the **state object** which holds aggregated state data for the saga.

```csharp
    .StartWith<MyMessage1>("topic", payload => key, payload => state, context => {
        // Get the current state of the Saga
        var state = context.State;

        // Abort the saga, cleanup resources and prevent future actions from firing
        context.Abort();

        // Complete the saga, cleanup resources, and execute the WhenComplete callback
        context.Complete();
    })
```

After specifying a start message, you can choose to continue the saga with any number of additional messages or continuances. You may specify any number of additional messages like this.

```csharp
    .ContinueWith<MyMessage2>("topic", payload => key, (context => { ... })
```

Finally you can specify a callback action to be executed when the saga is marked complete using the `context.Complete()` method described above. This method takes a reference to both a limited messageBus for publishing result messages and the state object with all the final state data. At this point you may choose to publish result messages or take other actions appropriate for your application. 

```csharp
    .WhenComplete((bus, state) => { ... })
);
```

### Examples

#### New User Account

My system is creating a new user account. Each user has a unique UserId that I can use to correlate messages together. When the user creates the account we receive a message that the account has been created, which several other subsystems are also subscribed to. When we create the account we need to wait for confirmation that necessary records have been created in other subsystems: The billing system needs to setup an automatic invoice schedule and the security system which must allocate a first-time password for the user. When these things are both done, we can send a notification message to the user that they are ready to log in with the new password.

```csharp
var token = messageBus.CreateSaga<NewUserState, int>(builder => builder
    .StartWith<NewUserMessage>("created", m => m.UserId, m => new NewUserState(m.UserId, m.Name, m.Email), context => {
        if (context.State.UserId <= 0)
            context.Abort();
    })
    .ContinueWith<BillingAccountMessage>("created", m => m.UserId, (context, m) => {
        if (!m.IsSuccess)
        {
            _log.Error("Billing account could not be created");
            context.Abort();
            return;
        }

        context.State.BillingInfo = m.Info;
        if (context.State.BillingInfo != null && context.State.SecurityInfo != null)
            context.Complete();
    })
    .ContinueWith<AccountSecurityMessage>("created", m => m.UserId, (context, m) => {
        if (!m.IsSuccess)
        {
            _log.Error("Security system could not create first-time password");
            context.Abort();
            return;
        }
        
        context.State.SecurityInfo = m.Info;
        if (context.State.BillingInfo != null && context.State.SecurityInfo != null)
            context.Complete();
    })
    .WhenComplete((bus, state) => {
        bus.Publish("welcomeEmail", new WelcomeEmailMessage {
            Text = $"Welcome {state.Name}! Your password is {state.SecurityInfo.Password}",
            Email = state.Email
        })
    })
);