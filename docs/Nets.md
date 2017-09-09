# Nets

**Warning**: Nets are an Experimental feature and will likely change in future releases based on usage and feedback.

Nets represent a directed acyclic graph of processing steps where each step can be executed asynchronously. Nets are built on top of Pub/Sub messaging. In fact, a `Net` wraps its own `IMessageBus` to facilitate all its work.

Start by creating a `NetBuilder`:

```csharp
var builder = new NetBuilder();
```

Now we need to add nodes to our Net. Each Net has a unique name. The Node must specify where it gets its inputs and what it does with them.

```csharp
// First node reads the Net input integer and outputs it as a string
builder.AddNode<int>("first")
    .ReadInput()
    .Transform<string>(i => t.ToString())

// Second node reads the output of the "first" node and writes it to
// the console
builder.AddNode<string>("second")
    .ReadOutputFrom("first")
    .Handle(s => Console.WriteLine(s));

// Third node also reads the output of "first" and writes to a file
builder.AddNode<string>("third")
    .ReadOutputFrom("first")
    .Handle(s => File.AppendAllText(fileName, s));

// Fourth node is an error-handling node which only handles errors
// from node "third"
builder.AddErrorNode<string>("fourth")
    .ReadOutputFrom("third")
    .Handle(e => _log.LogError("Error writing to file", e.Error));
```

By stringing together nodes like this with explicit dependencies, Acquaintance will be in charge of ordering operations and dispatching them across available worker threads.