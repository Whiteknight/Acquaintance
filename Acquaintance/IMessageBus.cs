using Acquaintance.Modules;
using Acquaintance.PubSub;
using Acquaintance.RequestResponse;
using Acquaintance.ScatterGather;
using Acquaintance.Threading;
using System;

namespace Acquaintance
{
    public interface IBusBase
    {
        IThreadPool ThreadPool { get; }
    }

    public interface ISubscribable : IBusBase
    {
        IDisposable Subscribe<TPayload>(string name, ISubscription<TPayload> subscription);
    }

    public interface IPublishable
    {
        void Publish<TPayload>(string name, TPayload payload);
    }

    public interface IPubSubBus : IPublishable, ISubscribable
    {

    }

    public interface IListenable : IBusBase
    {
        IDisposable Listen<TRequest, TResponse>(string name, IListener<TRequest, TResponse> listener);
        IDisposable Participate<TRequest, TResponse>(string name, IParticipant<TRequest, TResponse> listener);
        IDisposable Eavesdrop<TRequest, TResponse>(string name, ISubscription<Conversation<TRequest, TResponse>> subscriber);
    }

    public interface IRequestable
    {
        // Request-Response
        TResponse Request<TRequest, TResponse>(string name, TRequest request);
        IGatheredResponse<TResponse> Scatter<TRequest, TResponse>(string name, TRequest request);
    }

    public interface IReqResBus : IListenable, IRequestable
    {

    }

    public interface IMessageBus : IPubSubBus, IReqResBus, IDisposable
    {
        IModuleManager Modules { get; }

        // Runloops and Event Processing
        void RunEventLoop(Func<bool> shouldStop = null, int timeoutMs = 500);
        void EmptyActionQueue(int max);
    }
}
