using System;

namespace Acquaintance.RequestResponse
{
    public interface IListenerStore
    {
        IDisposable Listen<TRequest, TResponse>(string topic, IListener<TRequest, TResponse> listener);
        IListener<TRequest, TResponse> GetListener<TRequest, TResponse>(string topic);
        void RemoveListener<TRequest, TResponse>(string topic, IListener<TRequest, TResponse> listener);
    }
}