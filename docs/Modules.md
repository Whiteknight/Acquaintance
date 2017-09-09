# Modules

Modules are extension points for the Acquaintance MessageBus which add new functionality. Some of these are straight-forward applications of the existing messaging patterns, while others add completely new behavior.

Most modules consist of three parts: An `IMessageBusModule` class which is registered with the MessageBus, A series of extension methods on the MessageBus representing the API for that module, and a series of helper classes which implement the behavior.

* [Event Sources](EventSources.md)
* [Message Timer](Timer.md)
* [Sagas](Sagas.md)
* [Testing](Testing.md)