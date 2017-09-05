## Event Sources Module

**Event sources are experimental and may change in future releases**s

Acquaintance provides a mechanism to monitor a source of events and publish them at regular intervals. This mechanism is called "Event Sources". An Event Source is a callback which is invoked at regular intervals on a dedicated thread. The event source callback will do work and will be able to publish messages to the bus as they are available.

