namespace Acquaintance.Routing
{
    public interface IRouteRule
    {
    }

    public interface IRouteRule<TPayload> : IRouteRule
    {
        /// <summary>
        /// Get a new list of topics for the given topic and envelope
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="envelope"></param>
        /// <returns></returns>
        string[] GetRoute(string topic, Envelope<TPayload> envelope);
    }
}