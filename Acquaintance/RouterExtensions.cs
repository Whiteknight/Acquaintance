using System;
using System.Collections.Generic;
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
        
        public static IDisposable SetupPublishDistribution<TPayload>(this IPubSubBus messageBus, string topic, IEnumerable<string> topics)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            var rule = new RoundRobinDistributeRule<TPayload>(topics);
            return messageBus.PublishRouter.AddRule(topic, rule);
        }

        public static IDisposable SetupRequestDistribution<TRequest, TResponse>(this IReqResBus messageBus, string topic, IEnumerable<string> topics)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            var rule = new RoundRobinDistributeRule<TRequest>(topics);
            return messageBus.RequestRouter.AddRule<TRequest, TResponse>(topic, rule);
        }

        public static IDisposable SetupPublishByExamination<TPayload>(this IPubSubBus messageBus, string topic, Func<TPayload, string> getTopic)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(getTopic, nameof(getTopic));
            var rule = new PayloadExamineRule<TPayload>(getTopic);
            return messageBus.PublishRouter.AddRule<TPayload>(topic, rule);
        }

        public static IDisposable SetupRequestByExamination<TRequest, TResponse>(this IReqResBus messageBus, string topic, Func<TRequest, string> getTopic)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(getTopic, nameof(getTopic));
            var rule = new PayloadExamineRule<TRequest>(getTopic);
            return messageBus.RequestRouter.AddRule<TRequest, TResponse>(topic, rule);
        }

        public static IDisposable SetupScatterByExamination<TRequest, TResponse>(this IScatterGatherBus messageBus, string topic, Func<TRequest, string> getTopic)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(getTopic, nameof(getTopic));
            var rule = new PayloadExamineRule<TRequest>(getTopic);
            return messageBus.ScatterRouter.AddRule<TRequest, TResponse>(topic, rule);
        }
    }
}
