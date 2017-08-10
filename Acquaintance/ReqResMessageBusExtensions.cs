using Acquaintance.RequestResponse;
using System;
using System.Linq;
using Acquaintance.Utility;

namespace Acquaintance
{
    public static class ReqResMessageBusExtensions
    {
        public static TResponse Request<TRequest, TResponse>(this IReqResBus messageBus, string channelName, TRequest request)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));

            var envelope = messageBus.EnvelopeFactory.Create(channelName, request);
            return messageBus.RequestEnvelope<TRequest, TResponse>(envelope);
        }

        /// <summary>
        /// Make a request of the given type on the default channel
        /// </summary>
        /// <typeparam name="TRequest">The type of request object</typeparam>
        /// <typeparam name="TResponse">The type of response object</typeparam>
        /// <param name="messageBus">The message bus</param>
        /// <param name="request">The request object, which represents the input arguments to the RPC</param>
        /// <returns>A token representing the subscription which, when disposed, cancels the subscription</returns>
        public static TResponse Request<TRequest, TResponse>(this IReqResBus messageBus, TRequest request)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            return messageBus.Request<TRequest, TResponse>(string.Empty, request);
        }

        /// <summary>
        /// Make a request for cases where the type of request is not known until runtime. The bus
        /// attempts to infer the response type from metadata, and performs the request if possible.
        /// The request type must inherit from <see cref="IRequest{TResponse}"/> in order to make
        /// the necessary type inferences.
        /// </summary>
        /// <param name="messageBus">The message bus</param>
        /// <param name="channelName">The name of the channel</param>
        /// <param name="requestType">The runtime type of the request</param>
        /// <param name="request">The request object</param>
        /// <returns>The response object</returns>
        public static object Request(this IReqResBus messageBus, string channelName, Type requestType, object request)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            var requestInterface = requestType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));
            if (requestInterface == null)
                return null;

            var factoryMethod = messageBus.EnvelopeFactory.GetType()
                .GetMethod(nameof(messageBus.EnvelopeFactory.Create))
                .MakeGenericMethod(requestType);
            var envelope = factoryMethod.Invoke(messageBus.EnvelopeFactory, new[] { channelName, request, null });

            var responseType = requestInterface.GetGenericArguments().Single();

            var method = messageBus.GetType().GetMethod(nameof(messageBus.RequestEnvelope)).MakeGenericMethod(requestType, responseType);
            return method.Invoke(messageBus, new[] { envelope });
        }

        /// <summary>
        /// Build a listener to handle requests of the given type using common options.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request object</typeparam>
        /// <typeparam name="TResponse">The type of the response object</typeparam>
        /// <param name="messageBus">The message bus</param>
        /// <param name="build">The lambda function to build the listener</param>
        /// <returns>A token representing the subscription whic, when disposed, cancels the subscription</returns>
        public static IDisposable Listen<TRequest, TResponse>(this IReqResBus messageBus, Action<IChannelListenerBuilder<TRequest, TResponse>> build)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            var builder = new ListenerBuilder<TRequest, TResponse>(messageBus, messageBus.ThreadPool);
            build(builder);
            var listener = builder.BuildListener();
            var token = messageBus.Listen(builder.ChannelName, listener);
            return builder.WrapToken(token);
        }
    }
}