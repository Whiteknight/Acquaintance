using Acquaintance.RequestResponse;
using System;
using System.Linq;

namespace Acquaintance
{
    public static class ReqResMessageBusExtensions
    {
        public static TResponse Request<TRequest, TResponse>(this IRequestable messageBus, TRequest request)
        {
            return messageBus.Request<TRequest, TResponse>(string.Empty, request);
        }

        public static object Request(this IRequestable messageBus, string name, Type requestType, object request)
        {
            var requestInterface = requestType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));
            if (requestInterface == null)
                return null;
            var responseType = requestInterface.GetGenericArguments().Single();

            var method = messageBus.GetType().GetMethod("Request").MakeGenericMethod(requestType, responseType);
            return method.Invoke(messageBus, new[] { name, request });
        }

        public static IDisposable Listen<TRequest, TResponse>(this IReqResBus messageBus, Action<IChannelListenerBuilder<TRequest, TResponse>> build)
        {
            var builder = new ListenerBuilder<TRequest, TResponse>(messageBus, messageBus.ThreadPool);
            build(builder);
            var listener = builder.BuildListener();
            var token = messageBus.Listen(builder.ChannelName, listener);
            return builder.WrapToken(token);
        }
    }
}