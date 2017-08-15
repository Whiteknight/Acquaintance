namespace Acquaintance.Routing
{
    public interface IRequestRouteRule
    {    
    }

    public interface IRequestRouteRule<TRequest> : IRequestRouteRule
    {
        string GetRoute(string topic, Envelope<TRequest> envelope);
    }
}