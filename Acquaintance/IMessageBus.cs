using Acquaintance.RequestResponse;
using System;

namespace Acquaintance
{
    public interface ISubscribable
    {
        IDisposable Subscribe<TPayload>(string name, Action<TPayload> subscriber, Func<TPayload, bool> filter, SubscribeOptions options = null);
        IDisposable Subscribe<TRequest, TResponse>(string name, Func<TRequest, TResponse> subscriber, Func<TRequest, bool> filter, SubscribeOptions options = null)
            where TRequest : IRequest<TResponse>;
    }
    public interface IMessageBus : ISubscribable, IDisposable
    {
        // Worker Thread Management
        void StartWorkers(int numWorkers = 2);
        void StopWorkers();
        int StartDedicatedWorkerThread();
        void StopDedicatedWorkerThread(int id);

        // Sub-Sub Broadcasting
        void Publish<TPayload>(string name, TPayload payload);

        // Request-Response
        IBrokeredResponse<TResponse> Request<TRequest, TResponse>(string name, TRequest request)
            where TRequest : IRequest<TResponse>;
        //IBrokeredResponse<object> Request(string name, Type requestType, object request);

        // Runloops and Event Processing
        void RunEventLoop(Func<bool> shouldStop = null, int timeoutMs = 500);
        void EmptyActionQueue(int max);
    }
}