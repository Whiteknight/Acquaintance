using Acquaintance.RequestResponse;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance
{
    public static class ReqResMessageBusExtensions
    {
        public static IBrokeredResponse<TResponse> Request<TRequest, TResponse>(this IMessageBus messageBus, TRequest request)
            where TRequest : IRequest<TResponse>
        {
            return messageBus.Request<TRequest, TResponse>(string.Empty, request);
        }

        public static IBrokeredResponse<object> Request(this IMessageBus messageBus, string name, Type requestType, object request)
        {
            var requestInterface = requestType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));
            if (requestInterface == null)
                return new BrokeredResponse<object>(new List<object>());
            var responseType = requestInterface.GetGenericArguments().Single();

            var method = messageBus.GetType().GetMethod("Request").MakeGenericMethod(requestType, responseType);
            var response = method.Invoke(messageBus, new[] { name, request }) as IBrokeredResponse<object>;
            return response ?? new BrokeredResponse<object>(new List<object>());
        }

        public static IDisposable Subscribe<TRequest, TResponse>(this ISubscribable messageBus, string name, Func<TRequest, TResponse> subscriber, SubscribeOptions options = null)
            where TRequest : IRequest<TResponse>
        {
            return messageBus.Subscribe(name, subscriber, null, options);
        }

        public static IDisposable Subscribe<TRequest, TResponse>(this ISubscribable messageBus, Func<TRequest, TResponse> subscriber, Func<TRequest, bool> filter, SubscribeOptions options = null)
            where TRequest : IRequest<TResponse>
        {
            return messageBus.Subscribe(string.Empty, subscriber, null, options);
        }

        public static IDisposable Subscribe<TRequest, TResponse>(this ISubscribable messageBus, Func<TRequest, TResponse> subscriber, SubscribeOptions options = null)
            where TRequest : IRequest<TResponse>
        {
            return messageBus.Subscribe(string.Empty, subscriber, options);
        }
    }
}