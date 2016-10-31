using System;

namespace Acquaintance
{
    public sealed class SubscriptionToken : IDisposable
    {
        public SubscriptionToken(IChannel channel, Guid id)
        {
            Id = id;
            Channel = channel;
        }

        public IChannel Channel { get; }
        public Guid Id { get; }

        public void Dispose()
        {
            Channel.Unsubscribe(Id);
        }
    }
}