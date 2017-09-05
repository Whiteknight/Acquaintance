## Message Timer Module

The Message Timer module sets up a timer with the operating system. At each tick of the timer, a `MessageTimerEvent` is published. Interested components can subscribe to these timer messages to perform work at regular intervals.