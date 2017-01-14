namespace Acquaintance.RequestResponse
{
    /// <summary>
    /// Interface for Request objects to help give the dispatch system a hint about the expected request type.
    /// Using this interface on the TRequest parameter of IMessageBus.Request method helps ensure that methods
    /// with the correct response type will be selected.
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    public interface IRequest<TResponse>
    {
    }
}