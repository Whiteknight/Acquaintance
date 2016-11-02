using Acquaintance.PubSub;
using Acquaintance.RequestResponse;
using System;

namespace Acquaintance
{
    public interface ISubscribable
    {
        IDisposable Subscribe<TPayload>(string name, ISubscription<TPayload> subscription);
        SubscriptionFactory SubscriptionFactory { get; }
    }

    public interface IPublishable
    {
        // Sub-Sub Broadcasting
        void Publish<TPayload>(string name, TPayload payload);
    }

    public interface IPubSubBus : IPublishable, ISubscribable
    {

    }

    public interface IListenable
    {
        IDisposable Listen<TRequest, TResponse>(string name, IListener<TRequest, TResponse> listener, bool requestExclusivity = false);
        IDisposable Eavesdrop<TRequest, TResponse>(string name, Action<Conversation<TRequest, TResponse>> subscriber, Func<Conversation<TRequest, TResponse>, bool> filter, SubscribeOptions options = null);

        ListenerFactory ListenerFactory { get; }
    }

    public interface IRequestable
    {
        // Request-Response
        IBrokeredResponse<TResponse> Request<TRequest, TResponse>(string name, TRequest request);
    }

    public interface IReqResBus : IListenable, IRequestable
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
