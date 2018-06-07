using System;

namespace Acquaintance.Routing
{
    public interface IRequestTopicRouter
    {
        /// <summary>
        /// Route the request with the given topic and envelope. Return a new topic for forward the request 
        /// to
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="topic"></param>
        /// <param name="envelope"></param>
        /// <returns></returns>
        string RouteRequest<TRequest, TResponse>(string topic, Envelope<TRequest> envelope);

        /// <summary>
        /// Add a new routing rule for Request/Response
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="topic"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        IDisposable AddRule<TRequest, TResponse>(string topic, IRouteRule<TRequest> rule);
    }

    public interface IPublishTopicRouter
    {
        /// <summary>
        /// Route the publish with the given topics and envelope. Return a new list of topics to forward the 
        /// equest to
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="topics"></param>
        /// <param name="envelope"></param>
        /// <returns></returns>
        string[] RoutePublish<TPayload>(string[] topics, Envelope<TPayload> envelope);

        /// <summary>
        /// Add a new routing rule for Publish/Subscribe
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="topics"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        IDisposable AddRule<TPayload>(string[] topics, IRouteRule<TPayload> rule);
    }

    public interface IScatterTopicRouter
    {
        /// <summary>
        /// Route the scatter with the given topic and envelope. Return a new topic to forward the scatter
        /// to
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="topic"></param>
        /// <param name="envelope"></param>
        /// <returns></returns>
        string RouteScatter<TRequest, TResponse>(string topic, Envelope<TRequest> envelope);

        /// <summary>
        /// Add a new routing rule for Scatter/Gather
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="topic"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        IDisposable AddRule<TRequest, TResponse>(string topic, IRouteRule<TRequest> rule);
    }
}
