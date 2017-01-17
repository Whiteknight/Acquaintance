using System;
using System.Collections.Generic;

namespace Acquaintance.ScatterGather
{
    public interface IChannelParticipantBuilder<TRequest, TResponse>
    {
        IActionParticipantBuilder<TRequest, TResponse> WithChannelName(string name);
        IActionParticipantBuilder<TRequest, TResponse> OnDefaultChannel();
    }

    public interface IActionParticipantBuilder<TRequest, TResponse>
    {
        IThreadParticipantBuilder<TRequest, TResponse> Invoke(Func<TRequest, TResponse> participant, bool useWeakReference = false);
        IThreadParticipantBuilder<TRequest, TResponse> Invoke(Func<TRequest, IEnumerable<TResponse>> participant, bool useWeakReference = false);
        IThreadParticipantBuilder<TRequest, TResponse> Route(Action<RouteBuilder<TRequest, TResponse>> build);
        IThreadParticipantBuilder<TRequest, TResponse> TransformRequestTo<TTransformed>(string sourceChannelName, Func<TRequest, TTransformed> transform);
        IThreadParticipantBuilder<TRequest, TResponse> TransformResponseFrom<TSource>(string sourceChannelName, Func<TSource, TResponse> transform);
    }

    public interface IThreadParticipantBuilder<TRequest, TResponse>
    {
        IDetailsParticipantBuilder<TRequest, TResponse> Immediate();
        IDetailsParticipantBuilder<TRequest, TResponse> OnThread(int threadId);
        IDetailsParticipantBuilder<TRequest, TResponse> OnThreadPool();
        IDetailsParticipantBuilder<TRequest, TResponse> OnWorkerThread();
        IDetailsParticipantBuilder<TRequest, TResponse> OnDedicatedThread();
    }

    public interface IDetailsParticipantBuilder<TRequest, TResponse>
    {
        IDetailsParticipantBuilder<TRequest, TResponse> MaximumRequests(int maxRequests);
        IDetailsParticipantBuilder<TRequest, TResponse> WithFilter(Func<TRequest, bool> filter);
        IDetailsParticipantBuilder<TRequest, TResponse> WithTimeout(int timeoutMs);
        IDetailsParticipantBuilder<TRequest, TResponse> ModifyParticipant(Func<IParticipant<TRequest, TResponse>, IParticipant<TRequest, TResponse>> modify);
    }
}