﻿using Acquaintance.ScatterGather;
using Acquaintance.Utility;
using System;

namespace Acquaintance
{
    public static class ScatterGatherMessageBusExtensions
    {
        public static IGatheredResponse<TResponse> Scatter<TRequest, TResponse>(this IRequestable messageBus, TRequest request)
        {
            return messageBus.Scatter<TRequest, TResponse>(string.Empty, request);
        }

        public static IDisposable Participate<TRequest, TResponse>(this IReqResBus messageBus, Action<ParticipantBuilder<TRequest, TResponse>> build)
        {
            var builder = new ParticipantBuilder<TRequest, TResponse>(messageBus, messageBus.ThreadPool);
            build(builder);
            var listeners = builder.BuildParticipants();
            if (listeners.Count == 1)
                return messageBus.Participate(builder.ChannelName, listeners[0]);

            var tokens = new DisposableCollection();
            foreach (var listener in listeners)
            {
                var token = messageBus.Participate(builder.ChannelName, listener);
                tokens.Add(token);
            }
            return tokens;
        }

        //public static IDisposable Participate<TRequest, TResponse>(this IListenable messageBus, string name, Func<TRequest, TResponse> subscriber, Func<TRequest, bool> filter = null)
        //{
        //    var subscription = messageBus.ListenerFactory.CreateListener(subscriber, filter);
        //    return messageBus.Participate(name, subscription);
        //}

        //public static IDisposable Participate<TRequest, TResponse>(this IListenable messageBus, Func<TRequest, TResponse> subscriber, Func<TRequest, bool> filter = null)
        //{
        //    return messageBus.Participate(string.Empty, subscriber, null);
        //}

        // TODO: We need the ability for an IListener to return several responses, so we can route and transform properly
        //public static RequestRouter<TRequest, TResponse> ParticipateRouter<TRequest, TResponse>(this IReqResBus messageBus, string channelName)
        //{
        //    var router = new RequestRouter<TRequest, TResponse>(messageBus, channelName);
        //    var token = messageBus.Participate(channelName, router);
        //    router.SetToken(token);
        //    return router;
        //}

        //public static IDisposable ParticipateTransformRequest<TRequestIn, TRequestOut, TResponse>(this IReqResBus messageBus, string inName, Func<TRequestIn, TRequestOut> transform, Func<TRequestIn, bool> filter, string outName = null, ListenOptions options = null)
        //{
        //    return messageBus.Participate<TRequestIn, TResponse>(inName, rin =>
        //    {
        //        var rout = transform(rin);
        //        return messageBus.Scatter<TRequestOut, TResponse>(outName, rout);
        //    });
        //}

        //public static IDisposable ParticipateTransformResponse<TRequest, TResponseIn, TResponseOut>(this IReqResBus messageBus, string inName, Func<TResponseIn, TResponseOut> transform, Func<TRequest, bool> filter, string outName = null, ListenOptions options = null)
        //{
        //    return messageBus.Listen<TRequest, TResponseOut>(inName, request =>
        //    {
        //        var rin = messageBus.Participate<TRequest, TResponseIn>(outName, request);
        //        return transform(rin);
        //    });
        //}
    }
}