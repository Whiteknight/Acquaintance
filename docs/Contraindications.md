# Contraindications

Acquaintance is not a silver-bullet solution, and comparing it to other solutions and patterns is going to yield some positive and some negative points. It is important to consider the specific needs of your application before deciding whether to use Acquaintance or an alternative pattern or library. Some portions of Acquaintance may be desireable and others may be undesireable, depending on your particular solution.

At it's heart, Acquaintance runs contrary to some important Object-Oriented Programming principles and ideas. It conflicts with Dependency Injection, for example, and it suffers from the same discoverability problems that often make "Service Locator" an anti-pattern. Using Acquaintance for too much runs the risk of your message bus looking like a "God Object", and loose-coupling of this sort can make your code impervious to code analysis and refactoring tools. Adding Acquaintance into software which is a big mess and doesn't follow good OOP design best practices might just make things worse. Look for places in your system which demand loose-coupling and require one or more of the use-cases for which Acquaintance was designed.

* **Do**: Use the MessageBus to simplify long lists of injected dependency parameters
* **Don't**: Use the MessageBus as a God Object for handling all behavior

* **Do**: Wrap the MessageBus in an abstraction, such as a Facade or Adaptor Pattern, so that dependencies are clearly listed for better testability.
* **Don't**: Call the MessageBus all over your code, obscuring dependencies and creating spaghetti.

Message-passing in Acquaintance is cheaper than making a remote call to a separate service, but it's much more expensive than passing a message in a language like Objective-C or Erlang. You don't want to use Acquaintance for every single method call, and you want to keep performance in mind whenever you employ it. Acquaintance works best for passing messages between subsystems or modules, and is not intended to faciliate communication between nearby classes.

* **Do**: Use the MessageBus to communicate between modules and bounded subdomains, especially if they require pluggability or loose-coupling.
* **Don't**: Use the MessageBus to replace simple method calls in a single module or bounded subdomain

* **Do**: Keep performance in mind and call the MessageBus only when necessary
* **Don't**: Use the MessageBus when a simple direct method call would suffice.

Acquaintance works with threading primitives to provide a variety of dispatching behaviors, and it is entirely possible for you to configure Acquaintance to produce bottlenecks, resource starvation and soft-deadlocks if you are not paying enough attention to system design. Acquaintance may try to detect some of the most obvious issues, but it is ultimately your responsibility to keep your process running smoothly.

* **Do**: Use Acquaintance to help simplify multi-threaded programming and dispatching of work to multiple threads without needing Locks.
* **Don't**: Use Locks and strict ordering of operations to potentially create deadlocks.

* **Do**: Use the Immutable Object pattern for message contracts
* **Don't**: Make changes to the message/request object after it has been sent over the bus, or make changes to a message payload in a subscriber/listener/participant.

All this being said, there are many situations where Acquaintance can be much more help than hinderance, and many teams which can find real benefit in it's features.
