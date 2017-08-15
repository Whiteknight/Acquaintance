using System;
using Acquaintance.Routing;
using Acquaintance.Utility;

namespace Acquaintance
{
    public static class RouterExtensions
    {
        public static IDisposable SetupPublishRouting<TPayload>(this IPubSubBus messageBus, string topic, Action<FilterRouteBuilder<TPayload>> build)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            var router = BuildRouter(build);
            return messageBus.PublishRouter.AddRule(topic, router);
        }

        public static IDisposable SetupRequestRouting<TRequest, TResponse>(this IReqResBus messageBus, string topic, Action<FilterRouteBuilder<TRequest>> build)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            var router = BuildRouter(build);
            return messageBus.RequestRouter.AddRule<TRequest, TResponse>(topic, router);
        }

        public static IDisposable SetupScatterRouting<TRequest, TResponse>(this IScatterGatherBus messageBus, string topic, Action<FilterRouteBuilder<TRequest>> build)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            var router = BuildRouter(build);
            return messageBus.ScatterRouter.AddRule<TRequest, TResponse>(topic, router);
        }

        private static FilterRouteRule<T> BuildRouter<T>(Action<FilterRouteBuilder<T>> build)
        {
            Assert.ArgumentNotNull(build, nameof(build));

            var builder = new FilterRouteBuilder<T>();
            build(builder);
            var router = builder.Build();
            return router;
        }
    }
}
