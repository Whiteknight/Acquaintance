using System;
using System.Collections.Generic;

namespace Acquaintance.ScatterGather
{
    public interface IParticipantStore
    {
        IDisposable Participate<TRequest, TResponse>(string topic, IParticipant<TRequest, TResponse> participant);
        IEnumerable<IParticipant<TRequest, TResponse>> GetParticipants<TRequest, TResponse>(string topic);
        void RemoveParticipant<TRequest, TResponse>(string topic, IParticipant<TRequest, TResponse> participant);
    }
}