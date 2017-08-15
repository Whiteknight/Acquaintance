namespace Acquaintance.Routing
{
    public interface IScatterRouteRule
    {
    }

    public interface IScatterRouteRule<in TRequest> : IScatterRouteRule
    {
        string GetRoute(string topic, TRequest request);
    }
}