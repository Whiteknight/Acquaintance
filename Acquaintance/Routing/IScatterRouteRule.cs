namespace Acquaintance.Routing
{
    public interface IScatterRouteRule
    {
    }

    public interface IScatterRouteRule<TRequest> : IScatterRouteRule
    {
        string GetRoute(string topic, Envelope<TRequest> request);
    }
}