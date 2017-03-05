using System;

namespace Acquaintance.PubSub
{
    public interface ISubscription<TPayload>
    {
        void Publish(Envelope<TPayload> message);
        bool ShouldUnsubscribe { get; }
        Guid Id { get; set; }
    }
}