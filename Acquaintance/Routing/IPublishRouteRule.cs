namespace Acquaintance.Routing
{
    public interface IPublishRouteRule
    {
    }
    public interface IPublishRouteRule<TPayload> : IPublishRouteRule
    {
        string[] GetRoute(string topic, Envelope<TPayload> envelope);
    }
}