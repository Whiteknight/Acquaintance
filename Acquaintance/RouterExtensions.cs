using System;
using System.Collections.Generic;
using System.Linq;
using Acquaintance.Routing;
using Acquaintance.Utility;

namespace Acquaintance
{
    public static class RouterExtensions
    {
        public static IDisposable SetupPublishRouting<TPayload>(this IPubSubBus messageBus, Action<IFilterRouteBuilderMultiInput<TPayload>> build)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(build, nameof(build));

            var builder = new FilterRouteBuilder<TPayload>();
            build(builder);
            var router = builder.Build();
            return messageBus.PublishRouter.AddRule(builder.InTopics, router);
        }

        public static IDisposable SetupRequestRouting<TRequest, TResponse>(this IReqResBus messageBus, Action<IFilterRouteBuilderSingleInput<TRequest>> build)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(build, nameof(build));

            var builder = new FilterRouteBuilder<TRequest>();
            build(builder);
            var router = builder.Build();
            return messageBus.RequestRouter.AddRule<TRequest, TResponse>(builder.InTopics.FirstOrDefault(), router);
        }

        public static IDisposable SetupScatterRouting<TRequest, TResponse>(this IScatterGatherBus messageBus, Action<IFilterRouteBuilderSingleInput<TRequest>> build)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(build, nameof(build));

            var builder = new FilterRouteBuilder<TRequest>();
            build(builder);
            var router = builder.Build();
            return messageBus.ScatterRouter.AddRule<TRequest, TResponse>(builder.InTopics.FirstOrDefault(), router);
        }
        
        public static IDisposable SetupPublishDistribution<TPayload>(this IPubSubBus messageBus, string inTopic, IEnumerable<string> outTopics)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            var rule = new RoundRobinDistributeRule<TPayload>(outTopics);
            return messageBus.PublishRouter.AddRule(new [] { inTopic }, rule);
        }

        public static IDisposable SetupPublishDistribution<TPayload>(this IPubSubBus messageBus, string[] inTopics, IEnumerable<string> outTopics)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            var rule = new RoundRobinDistributeRule<TPayload>(outTopics);
            return messageBus.PublishRouter.AddRule(inTopics, rule);
        }

        public static IDisposable SetupRequestDistribution<TRequest, TResponse>(this IReqResBus messageBus, string inTopics, IEnumerable<string> outTopics)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            var rule = new RoundRobinDistributeRule<TRequest>(outTopics);
            return messageBus.RequestRouter.AddRule<TRequest, TResponse>(inTopics, rule);
        }

        public static IDisposable SetupPublishByExamination<TPayload>(this IPubSubBus messageBus, string inTopic, Func<TPayload, string> getTopic)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(getTopic, nameof(getTopic));
            var rule = new PayloadExamineRule<TPayload>(getTopic);
            return messageBus.PublishRouter.AddRule(new [] { inTopic }, rule);
        }

        public static IDisposable SetupPublishByExamination<TPayload>(this IPubSubBus messageBus, string[] inTopics, Func<TPayload, string> getTopic)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(getTopic, nameof(getTopic));
            var rule = new PayloadExamineRule<TPayload>(getTopic);
            return messageBus.PublishRouter.AddRule(inTopics, rule);
        }

        public static IDisposable SetupRequestByExamination<TRequest, TResponse>(this IReqResBus messageBus, string inTopic, Func<TRequest, string> getTopic)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(getTopic, nameof(getTopic));
            var rule = new PayloadExamineRule<TRequest>(getTopic);
            return messageBus.RequestRouter.AddRule<TRequest, TResponse>(inTopic, rule);
        }

        public static IDisposable SetupScatterByExamination<TRequest, TResponse>(this IScatterGatherBus messageBus, string inTopic, Func<TRequest, string> getTopic)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(getTopic, nameof(getTopic));
            var rule = new PayloadExamineRule<TRequest>(getTopic);
            return messageBus.ScatterRouter.AddRule<TRequest, TResponse>(inTopic, rule);
        }
    }
}
