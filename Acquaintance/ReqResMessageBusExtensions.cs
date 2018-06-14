using Acquaintance.RequestResponse;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Acquaintance.Scanning;
using Acquaintance.Utility;

namespace Acquaintance
{
    public static class ReqResMessageBusExtensions
    {
        public static IRequest<TResponse> Request<TRequest, TResponse>(this IReqResBus messageBus, string topic, TRequest requestPayload)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));

            var envelope = messageBus.EnvelopeFactory.Create(topic, requestPayload);
            return messageBus.RequestEnvelope<TRequest, TResponse>(envelope);
        }

        public static TResponse RequestWait<TRequest, TResponse>(this IReqResBus messageBus, string topic, TRequest requestPayload)
        {
            return Request<TRequest, TResponse>(messageBus, topic, requestPayload).GetResponseOrWait();
        }

        /// <summary>
        /// Make a request of the given type on the default channel
        /// </summary>
        /// <typeparam name="TRequest">The type of request object</typeparam>
        /// <typeparam name="TResponse">The type of response object</typeparam>
        /// <param name="messageBus">The message bus</param>
        /// <param name="requestPayload">The request object, which represents the input arguments to the RPC</param>
        /// <returns>A token representing the subscription which, when disposed, cancels the subscription</returns>
        public static IRequest<TResponse> Request<TRequest, TResponse>(this IReqResBus messageBus, TRequest requestPayload)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            return messageBus.Request<TRequest, TResponse>(string.Empty, requestPayload);
        }

        public static TResponse RequestWait<TRequest, TResponse>(this IReqResBus messageBus, TRequest requestPayload)
        {
            return Request<TRequest, TResponse>(messageBus, requestPayload).GetResponseOrWait();
        }

        public static Task<TResponse> RequestAsync<TRequest, TResponse>(this IReqResBus messageBus, string topic, TRequest requestPayload)
        {
            var request = Request<TRequest, TResponse>(messageBus, topic, requestPayload);
            return request.GetResponseAsync();
        }

        public static Task<TResponse> RequestAsync<TRequest, TResponse>(this IReqResBus messageBus, TRequest requestPayload)
        {
            var request = Request<TRequest, TResponse>(messageBus, requestPayload);
            return request.GetResponseAsync();
        }

        /// <summary>
        /// Make a request for cases where the type of request is not known until runtime. The bus
        /// attempts to infer the response type from metadata, and performs the request if possible.
        /// The request type must inherit from <see cref="IRequestWithResponse{TResponse}"/> in order to make
        /// the necessary type inferences.
        /// </summary>
        /// <param name="messageBus">The message bus</param>
        /// <param name="topic">The name of the channel</param>
        /// <param name="requestType">The runtime type of the request</param>
        /// <param name="request">The request object</param>
        /// <returns>The response object</returns>
        public static object Request(this IReqResBus messageBus, string topic, Type requestType, object request)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            var requestInterface = requestType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestWithResponse<>));
            if (requestInterface == null)
                return null;

            var factoryMethod = messageBus.EnvelopeFactory.GetType()
                .GetMethod(nameof(messageBus.EnvelopeFactory.Create))
                .MakeGenericMethod(requestType);
            var envelope = factoryMethod.Invoke(messageBus.EnvelopeFactory, new[] { new [] { topic }, request, null });

            var responseType = requestInterface.GetGenericArguments().Single();

            var method = messageBus.GetType().GetMethod(nameof(messageBus.RequestEnvelope)).MakeGenericMethod(requestType, responseType);
            var waiter = (Request)method.Invoke(messageBus, new[] { envelope });
            waiter.WaitForResponse();
            waiter.ThrowExceptionIfError();
            return waiter.GetResponseObject();
        }

        /// <summary>
        /// Build a listener to handle requests of the given type using common options.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request object</typeparam>
        /// <typeparam name="TResponse">The type of the response object</typeparam>
        /// <param name="messageBus">The message bus</param>
        /// <param name="build">The lambda function to build the listener</param>
        /// <returns>A token representing the subscription whic, when disposed, cancels the subscription</returns>
        public static IDisposable Listen<TRequest, TResponse>(this IReqResBus messageBus, Action<ITopicListenerBuilder<TRequest, TResponse>> build)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            var builder = new ListenerBuilder<TRequest, TResponse>(messageBus, messageBus.WorkerPool);
            build(builder);
            var listener = builder.BuildListener();
            var token = messageBus.Listen(builder.Topic, listener);
            return builder.WrapToken(token);
        }

        /// <summary>
        /// Get a typed request channel to simplify request/response calls
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="messageBus"></param>
        /// <returns></returns>
        public static RequestChannelProxy<TRequest, TResponse> GetRequestChannel<TRequest, TResponse>(this IReqResBus messageBus)
        {
            return new RequestChannelProxy<TRequest, TResponse>(messageBus);
        }

        public static WrappedFunction<TRequest, TResponse> WrapFunction<TRequest, TResponse>(this IReqResBus messageBus, Func<TRequest, TResponse> func, Action<IThreadListenerBuilder<TRequest, TResponse>> build)
        {
            return new RequestFuncWrapper<TRequest, TResponse>().WrapFunction(messageBus, func, build);
        }

        public static IDisposable AutoWireupListeners(this IReqResBus messageBus, object obj, bool useWeakReferences = false)
        {
            var tokens = new ListenerScanner(messageBus, messageBus.Logger).DetectAndWireUpAll(obj, useWeakReferences);
            return new DisposableCollection(tokens);
        }

        public static IDisposable ListenUntyped(this IReqResBus messageBus, Type requestType, Type responseType, string topic, Func<object, object> handle, bool useWeakReferences = false)
        {
            return new UntypedListenerBuilder(messageBus).ListenUntyped(requestType, responseType, topic, handle, useWeakReferences);
        }

        public static IDisposable ListenUntyped(this IReqResBus messageBus, Type requestType, Type responseType, string topic, object target, MethodInfo listener, bool useWeakReferences = false)
        {
            return new UntypedListenerBuilder(messageBus).ListenUntyped(requestType, responseType, topic, target, listener, useWeakReferences);
        }

        public static IDisposable ListenEnvelopeUntyped(this IReqResBus messageBus, Type requestType, Type responseType, string topic, object target, MethodInfo listener, bool useWeakReferences = false)
        {
            return new UntypedListenerBuilder(messageBus).ListenEnvelopeUntyped(requestType, responseType, topic, target, listener, useWeakReferences);
        }
    }
}