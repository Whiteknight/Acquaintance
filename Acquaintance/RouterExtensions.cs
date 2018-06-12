using System;
using System.Collections.Generic;
using System.Linq;
using Acquaintance.Routing;
using Acquaintance.Utility;

namespace Acquaintance
{
    public static class RouterExtensions
    {
        /// <summary>
        /// Add predicate-based routing rules for Publish operations
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="messageBus"></param>
        /// <param name="build"></param>
        /// <returns></returns>
        public static IDisposable SetupPublishRouting<TPayload>(this IPubSubBus messageBus, Action<IFilterRouteBuilderMultiInput<TPayload>> build)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(build, nameof(build));

            var builder = new FilterRouteBuilder<TPayload>();
            build(builder);
            var router = builder.Build();
            return messageBus.PublishRouter.AddPublishRouteRule(builder.InTopics, router);
        }

        /// <summary>
        /// Add predicate-based routing rules for Request/Response operations
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="messageBus"></param>
        /// <param name="build"></param>
        /// <returns></returns>
        public static IDisposable SetupRequestRouting<TRequest, TResponse>(this IReqResBus messageBus, Action<IFilterRouteBuilderSingleInput<TRequest>> build)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(build, nameof(build));

            var builder = new FilterRouteBuilder<TRequest>();
            build(builder);
            var router = builder.Build();
            return messageBus.RequestRouter.AddRequestRouteRule<TRequest, TResponse>(builder.InTopics.FirstOrDefault(), router);
        }

        /// <summary>
        /// Add predicate-based routing rules for Scatter/Gather operations
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="messageBus"></param>
        /// <param name="build"></param>
        /// <returns></returns>
        public static IDisposable SetupScatterRouting<TRequest, TResponse>(this IScatterGatherBus messageBus, Action<IFilterRouteBuilderSingleInput<TRequest>> build)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(build, nameof(build));

            var builder = new FilterRouteBuilder<TRequest>();
            build(builder);
            var router = builder.Build();
            return messageBus.ScatterRouter.AddScatterRouteRule<TRequest, TResponse>(builder.InTopics.FirstOrDefault(), router);
        }
        
        /// <summary>
        /// Setup round-robin distribution rules for Publish operations
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="messageBus"></param>
        /// <param name="inTopic"></param>
        /// <param name="outTopics"></param>
        /// <returns></returns>
        public static IDisposable SetupPublishDistribution<TPayload>(this IPubSubBus messageBus, string inTopic, IEnumerable<string> outTopics)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            var rule = new RoundRobinDistributeRule<TPayload>(outTopics);
            return messageBus.PublishRouter.AddPublishRouteRule(new [] { inTopic }, rule);
        }

        /// <summary>
        /// Setup round-robin distribution rules for Publish operations
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="messageBus"></param>
        /// <param name="inTopics"></param>
        /// <param name="outTopics"></param>
        /// <returns></returns>
        public static IDisposable SetupPublishDistribution<TPayload>(this IPubSubBus messageBus, string[] inTopics, IEnumerable<string> outTopics)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            var rule = new RoundRobinDistributeRule<TPayload>(outTopics);
            return messageBus.PublishRouter.AddPublishRouteRule(inTopics, rule);
        }

        /// <summary>
        /// Setup round-robin distribution rules for Request/Response channels
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="messageBus"></param>
        /// <param name="inTopics"></param>
        /// <param name="outTopics"></param>
        /// <returns></returns>
        public static IDisposable SetupRequestDistribution<TRequest, TResponse>(this IReqResBus messageBus, string inTopics, IEnumerable<string> outTopics)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            var rule = new RoundRobinDistributeRule<TRequest>(outTopics);
            return messageBus.RequestRouter.AddRequestRouteRule<TRequest, TResponse>(inTopics, rule);
        }

        /// <summary>
        /// Setup rules to route the Published message by inspecting the contents of the payload
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="messageBus"></param>
        /// <param name="inTopic"></param>
        /// <param name="getTopic"></param>
        /// <returns></returns>
        public static IDisposable SetupPublishByExamination<TPayload>(this IPubSubBus messageBus, string inTopic, Func<TPayload, string> getTopic)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(getTopic, nameof(getTopic));
            var rule = new PayloadExamineRule<TPayload>(getTopic);
            return messageBus.PublishRouter.AddPublishRouteRule(new [] { inTopic }, rule);
        }

        /// <summary>
        /// Setup rules to route the Published message by inspecting the contents of the payload
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="messageBus"></param>
        /// <param name="inTopics"></param>
        /// <param name="getTopic"></param>
        /// <returns></returns>
        public static IDisposable SetupPublishByExamination<TPayload>(this IPubSubBus messageBus, string[] inTopics, Func<TPayload, string> getTopic)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(getTopic, nameof(getTopic));
            var rule = new PayloadExamineRule<TPayload>(getTopic);
            return messageBus.PublishRouter.AddPublishRouteRule(inTopics, rule);
        }

        /// <summary>
        /// Setup rules to route the Request/Response channel by inspecting the contents of the request payload
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="messageBus"></param>
        /// <param name="inTopic"></param>
        /// <param name="getTopic"></param>
        /// <returns></returns>
        public static IDisposable SetupRequestByExamination<TRequest, TResponse>(this IReqResBus messageBus, string inTopic, Func<TRequest, string> getTopic)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(getTopic, nameof(getTopic));
            var rule = new PayloadExamineRule<TRequest>(getTopic);
            return messageBus.RequestRouter.AddRequestRouteRule<TRequest, TResponse>(inTopic, rule);
        }

        /// <summary>
        /// Setup rules to route the Scatter/Gather channel by inspecting the contents of the Scatter payload
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="messageBus"></param>
        /// <param name="inTopic"></param>
        /// <param name="getTopic"></param>
        /// <returns></returns>
        public static IDisposable SetupScatterByExamination<TRequest, TResponse>(this IScatterGatherBus messageBus, string inTopic, Func<TRequest, string> getTopic)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(getTopic, nameof(getTopic));
            var rule = new PayloadExamineRule<TRequest>(getTopic);
            return messageBus.ScatterRouter.AddScatterRouteRule<TRequest, TResponse>(inTopic, rule);
        }
    }
}
