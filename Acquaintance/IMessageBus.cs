using System;

namespace Acquaintance
{
    public interface ISubscribable
    {
        IDisposable Subscribe<TPayload>(string name, Action<TPayload> subscriber, Func<TPayload, bool> filter, SubscribeOptions options = null);
    }

    public interface IPublishable
    {
        // Sub-Sub Broadcasting
        void Publish<TPayload>(string name, TPayload payload);
    }

    public interface IPubSubBus : IPublishable, ISubscribable
    {

    }

    public interface IRequestListenable
    {
        IDisposable Listen<TRequest, TResponse>(string name, Func<TRequest, TResponse> subscriber, Func<TRequest, bool> filter, SubscribeOptions options = null);
    }

    public interface IRequestable
    {
        // Request-Response
        IBrokeredResponse<TResponse> Request<TRequest, TResponse>(string name, TRequest request);
    }

    public interface IReqResBus : IRequestListenable, IRequestable
    {

    }

    public interface IMessageBus : IPubSubBus, IReqResBus, IDisposable
    {
        // Worker Thread Management
        void StartWorkers(int numWorkers = 2);
        void StopWorkers();
        int StartDedicatedWorkerThread();
        void StopDedicatedWorkerThread(int id);

        // Runloops and Event Processing
        void RunEventLoop(Func<bool> shouldStop = null, int timeoutMs = 500);
        void EmptyActionQueue(int max);
    }
}