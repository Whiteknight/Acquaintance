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

        public IChannel Channel { get; private set; }
        public Guid Id { get; private set; }

        public void Dispose()
        {
            Channel.Unsubscribe(Id);
        }
    }
}