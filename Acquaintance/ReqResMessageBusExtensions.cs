using Acquaintance.RequestResponse;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance
{
    public static class ReqResMessageBusExtensions
    {
        public static IBrokeredResponse<TResponse> Request<TRequest, TResponse>(this IRequestable messageBus, TRequest request)
        {
            return messageBus.Request<TRequest, TResponse>(string.Empty, request);
        }

        public static IBrokeredResponse<object> Request(this IRequestable messageBus, string name, Type requestType, object request)
        {
            var requestInterface = requestType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));
            if (requestInterface == null)
                return new BrokeredResponse<object>(new List<object>());
            var responseType = requestInterface.GetGenericArguments().Single();

            var method = messageBus.GetType().GetMethod("Request").MakeGenericMethod(requestType, responseType);
            var response = method.Invoke(messageBus, new[] { name, request }) as IBrokeredResponse<object>;
            return response ?? new BrokeredResponse<object>(new List<object>());
        }

        public static IDisposable Listen<TRequest, TResponse>(this IListenable messageBus, string name, Func<TRequest, TResponse> subscriber, SubscribeOptions options = null)
        {
            return messageBus.Listen(name, subscriber, null, options);
        }

        public static IDisposable Listen<TRequest, TResponse>(this IListenable messageBus, Func<TRequest, TResponse> subscriber, Func<TRequest, bool> filter, SubscribeOptions options = null)
        {
            return messageBus.Listen(string.Empty, subscriber, null, options);
        }

        public static IDisposable Listen<TRequest, TResponse>(this IListenable messageBus, Func<TRequest, TResponse> subscriber, SubscribeOptions options = null)
        {
            return messageBus.Listen(string.Empty, subscriber, null, options);
        }

        public static IDisposable Eavesdrop<TRequest, TResponse>(this IListenable messageBus, Action<Conversation<TRequest, TResponse>> subscriber, Func<Conversation<TRequest, TResponse>, bool> filter, SubscribeOptions options = null)
        {
            return messageBus.Eavesdrop(string.Empty, subscriber, filter, options);
        }

        public static IDisposable Eavesdrop<TRequest, TResponse>(this IListenable messageBus, string name, Action<Conversation<TRequest, TResponse>> subscriber, SubscribeOptions options = null)
        {
            return messageBus.Eavesdrop(name, subscriber, null, options);
        }

        public static IDisposable Eavesdrop<TRequest, TResponse>(this IListenable messageBus, Action<Conversation<TRequest, TResponse>> subscriber, SubscribeOptions options = null)
        {
            return messageBus.Eavesdrop(string.Empty, subscriber, null, options);
        }
    }
}