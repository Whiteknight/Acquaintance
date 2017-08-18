Unlike the C# `event` mechanism, the message bus solves the chicken-and-egg problem by allowing the event producer and the event consumers to be created in any order, at any time. 

Unlike simple callback delegates, Acquaintance will automatically dispatch your request onto a worker thread so it doesn't block other processing. 