namespace Acquaintance.Routing
{
    public interface IRouteRule
    {
    }
    public interface IRouteRule<TPayload> : IRouteRule
    {
        string[] GetRoute(string topic, Envelope<TPayload> envelope);
    }
}