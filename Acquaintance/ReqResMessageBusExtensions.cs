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
            var subscription = messageBus.ListenerFactory.CreateListener(subscriber, filter, options);
            return messageBus.Listen(name, subscription);
        }

        public static IDisposable Listen<TRequest, TResponse>(this IListenable messageBus, Func<TRequest, TResponse> subscriber, Func<TRequest, bool> filter = null, ListenOptions options = null)
        {
            return messageBus.Listen(string.Empty, subscriber, null, options);
        }

        public static RequestRouter<TRequest, TResponse> RequestRouter<TRequest, TResponse>(this IReqResBus messageBus, string channelName)
        {
            var router = new RequestRouter<TRequest, TResponse>(messageBus, channelName);
            var token = messageBus.Listen(channelName, router);
            router.SetToken(token);
            return router;
        }

        public static IDisposable ListenTransformRequest<TRequestIn, TRequestOut, TResponse>(this IReqResBus messageBus, string inName, Func<TRequestIn, TRequestOut> transform, Func<TRequestIn, bool> filter, string outName = null, ListenOptions options = null)
        {
            return messageBus.Listen<TRequestIn, TResponse>(inName, rin =>
            {
                var rout = transform(rin);
                return messageBus.Request<TRequestOut, TResponse>(outName, rout);
            });
        }

        public static IDisposable ListenTransformResponse<TRequest, TResponseIn, TResponseOut>(this IReqResBus messageBus, string inName, Func<TResponseIn, TResponseOut> transform, Func<TRequest, bool> filter, string outName = null, ListenOptions options = null)
        {
            return messageBus.Listen<TRequest, TResponseOut>(inName, request =>
            {
                var rin = messageBus.Request<TRequest, TResponseIn>(outName, request);
                return transform(rin);
            });
        }
    }
}