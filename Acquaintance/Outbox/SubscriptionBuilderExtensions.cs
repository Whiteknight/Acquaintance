using Acquaintance.PubSub;
using Acquaintance.Utility;

namespace Acquaintance.Outbox
{
    public static class SubscriptionBuilderExtensions
    {
        /// <summary>
        /// Send the message to a custom outbox, where delivery can be completely controlled. The outbox
        /// will be the final destination of the message.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="outbox"></param>
        /// <returns></returns>
        public static IThreadSubscriptionBuilder<TPayload> SendToOutbox<TPayload>(this IActionSubscriptionBuilder<TPayload> builder, IOutbox<TPayload> outbox)
        {
            Assert.ArgumentNotNull(builder, nameof(builder));
            Assert.ArgumentNotNull(outbox, nameof(outbox));
            return builder.UseCustomSubscriber(new OutboxSubscriberReference<TPayload>(outbox));
        }

        /// <summary>
        /// Use an outbox as a temporary cache between the publisher and the final destination of the message.
        /// The outbox will be a stage in the delivery pipeline, invoked after filtering and liveliness checks, 
        /// and will flush to the next stage in the pipeline.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="outboxFactory"></param>
        /// <returns></returns>
        public static IDetailsSubscriptionBuilder<TPayload> UseOutbox<TPayload>(this IDetailsSubscriptionBuilder<TPayload> builder, IOutboxFactory outboxFactory)
        {
            Assert.ArgumentNotNull(builder, nameof(builder));
            Assert.ArgumentNotNull(outboxFactory, nameof(outboxFactory));
            return builder.WrapSubscriptionBase(s => OutboxSubscription<TPayload>.WrapSubscription(s, outboxFactory));
        }
    }
}
