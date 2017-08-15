using System;

namespace Acquaintance.Routing
{
    public interface IRequestTopicRouter
    {
        string RouteRequest<TRequest, TResponse>(string topic, Envelope<TRequest> envelope);
        IDisposable AddRule<TRequest, TResponse>(string topic, IRequestRouteRule<TRequest> rule);
    }

    public interface IPublishTopicRouter
    {
        string[] RoutePublish<TPayload>(string topic, Envelope<TPayload> envelope);
        IDisposable AddRule<TPayload>(string topic, IPublishRouteRule<TPayload> rule);
    }

    public interface IScatterTopicRouter
    {
        string RouteScatter<TRequest, TResponse>(string topic, Envelope<TRequest> envelope);
        IDisposable AddRule<TRequest, TResponse>(string topic, IScatterRouteRule<TRequest> rule);
    }
}
