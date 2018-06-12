using Acquaintance.PubSub;
using Acquaintance.Utility;

namespace Acquaintance.Outbox
{
    public static class SubscriptionBuilderExtensions
    {
        /// <summary>
        /// Use an outbox as a temporary cache between the publisher and the final destination of the message.
        /// The outbox will be a stage in the delivery pipeline, invoked after filtering and liveliness checks, 
        /// and will flush to the next stage in the pipeline.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="outbox"></param>
        /// <returns></returns>
        public static IDetailsSubscriptionBuilder<TPayload> UseOutbox<TPayload>(this IDetailsSubscriptionBuilder<TPayload> builder, IOutbox<TPayload> outbox)
        {
            Assert.ArgumentNotNull(builder, nameof(builder));
            Assert.ArgumentNotNull(outbox, nameof(outbox));

            return builder.WrapSubscriptionBase(s => OutboxSubscription<TPayload>.WrapSubscription(builder.MessageBus, s, outbox));
        }

        /// <summary>
        /// Use an In-Memory Outbox implementation
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="builder"></param>
        /// <param name="maxMessages">The maximum number of messages to hold. If 0, there is no limit</param>
        /// <returns></returns>
        public static IDetailsSubscriptionBuilder<TPayload> UseInMemoryOutbox<TPayload>(this IDetailsSubscriptionBuilder<TPayload> builder, int maxMessages = 100)
        {
            return UseOutbox(builder, new InMemoryOutbox<TPayload>(maxMessages));
        }
    }
}
