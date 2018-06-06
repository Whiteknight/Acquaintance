# Contributing

We welcome contributions, PRs, suggestions, advice and other feedback from anybody willing to get involved.

Open a PR or Issue on the [Gitub project page](http://github.com/Whiteknight/Acquaintance) to start a conversation.

## Guidelines

The goal of Acquaintance is not just to provide messaging features but to try and provide them in the right way.

* Acquaintance really shouldn't have any dependencies. It is a foundational piece. We don't want Acquaintance contributing to DLL Hell.
* We try to make good use of well-known Design Patterns and SOLID design, and we refactor relentlessly to bring us there.
* Nearly everything should be unit tested. That's how we know a feature works as advertised. Considering the central role Acquaintance may play in user applications, it should be proven to just work.
* Just about everything should be thread-safe, preferrably without locks. Acquaintance gets the multi-threading details right so applications don't need to worry about them.

