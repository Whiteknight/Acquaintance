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

        public static IDisposable Listen<TRequest, TResponse>(this IListenable messageBus, string name, Func<TRequest, TResponse> subscriber, Func<TRequest, bool> filter = null, ListenOptions options = null)
        {
            var subscription = messageBus.ListenerFactory.CreateListener<TRequest, TResponse>(subscriber, filter, options);
            return messageBus.Listen(name, subscription);
        }

        public static IDisposable Listen<TRequest, TResponse>(this IListenable messageBus, Func<TRequest, TResponse> subscriber, Func<TRequest, bool> filter = null, ListenOptions options = null)
        {
            return messageBus.Listen(string.Empty, subscriber, null, options);
        }

        public static IGatheredResponse<TResponse> Scatter<TRequest, TResponse>(this IRequestable messageBus, TRequest request)
        {
            return messageBus.Scatter<TRequest, TResponse>(string.Empty, request);
        }

        public static IDisposable Participate<TRequest, TResponse>(this IListenable messageBus, string name, Func<TRequest, TResponse> subscriber, Func<TRequest, bool> filter = null, ListenOptions options = null)
        {
            var subscription = messageBus.ListenerFactory.CreateListener<TRequest, TResponse>(subscriber, filter, options);
            return messageBus.Participate(name, subscription);
        }

        public static IDisposable Participate<TRequest, TResponse>(this IListenable messageBus, Func<TRequest, TResponse> subscriber, Func<TRequest, bool> filter = null, ListenOptions options = null)
        {
            return messageBus.Participate(string.Empty, subscriber, null, options);
        }

        public static IDisposable Eavesdrop<TRequest, TResponse>(this IListenable messageBus, string name, Action<Conversation<TRequest, TResponse>> subscriber, Func<Conversation<TRequest, TResponse>, bool> filter = null, SubscribeOptions options = null)
        {
            var subscription = messageBus.SubscriptionFactory.CreateSubscription(subscriber, filter, options);
            return messageBus.Eavesdrop(name, subscription);
        }

        public static IDisposable Eavesdrop<TRequest, TResponse>(this IListenable messageBus, Action<Conversation<TRequest, TResponse>> subscriber, Func<Conversation<TRequest, TResponse>, bool> filter = null, SubscribeOptions options = null)
        {
            return messageBus.Eavesdrop(string.Empty, subscriber, filter, options);
        }

        public static RequestRouter<TRequest, TResponse> RequestRouter<TRequest, TResponse>(this IReqResBus messageBus, string channelName)
        {
            var router = new RequestRouter<TRequest, TResponse>(messageBus, channelName);
            var token = messageBus.Listen(channelName, router);
            router.SetToken(token);
            return router;
        }
    }
}