using System;
using Acquaintance.RequestResponse;

namespace Acquaintance
{
    public static class ReqResMessageBusExtensions
    {
        public static IBrokeredResponse<TResponse> Request<TRequest, TResponse>(this IMessageBus messageBus, TRequest request)
            where TRequest : IRequest<TResponse>
        {
            return messageBus.Request<TRequest, TResponse>(string.Empty, request);
        }

        //public static IBrokeredResponse<object> Request(this IMessageBus messageBus, Type requestType, object request)
        //{
        //    return messageBus.Request(string.Empty, requestType, request);
        //}

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