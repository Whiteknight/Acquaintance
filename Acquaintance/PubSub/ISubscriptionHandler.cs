namespace Acquaintance.PubSub
{
    /// <summary>
    /// A handler object which is preserved against GC collection and may hold state. ISubscriber
    /// Is expected to take care of thread dispatching, but this type can be dispatched according
    /// to normal dispatch rules. Use ISubscriptionHandler any time you need a stateful callback
    /// object, but still want IMessageBus to handle thread dispatching.
    /// </summary>
    /// <typeparam name="TPayload"></typeparam>
    public interface ISubscriptionHandler<TPayload>
    {
        void Handle(Envelope<TPayload> message);
    }
}
