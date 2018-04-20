using System;

namespace Acquaintance.Routing
{
    public interface IRequestTopicRouter
    {
        string RouteRequest<TRequest, TResponse>(string topic, Envelope<TRequest> envelope);
        IDisposable AddRule<TRequest, TResponse>(string topic, IRouteRule<TRequest> rule);
    }

    public interface IPublishTopicRouter
    {
        string[] RoutePublish<TPayload>(string[] topics, Envelope<TPayload> envelope);
        IDisposable AddRule<TPayload>(string[] topics, IRouteRule<TPayload> rule);
    }

    public interface IScatterTopicRouter
    {
        string RouteScatter<TRequest, TResponse>(string topic, Envelope<TRequest> envelope);
        IDisposable AddRule<TRequest, TResponse>(string topic, IRouteRule<TRequest> rule);
    }
}
