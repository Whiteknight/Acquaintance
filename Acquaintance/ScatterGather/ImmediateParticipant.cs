using System;

namespace Acquaintance.ScatterGather
{
    /// <summary>
    /// Executes the participant reference on the current thread
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public class ImmediateParticipant<TRequest, TResponse> : IParticipant<TRequest, TResponse>
    {
        private readonly IParticipantReference<TRequest, TResponse> _func;
        private readonly string _name;

        public ImmediateParticipant(IParticipantReference<TRequest, TResponse> func, string name)
        {
            _func = func;
            _name = name;
        }

        public Guid Id { get; set; }
        public string Name => string.IsNullOrEmpty(_name) ? Id.ToString() : _name;
        public bool ShouldStopParticipating => !_func.IsAlive;

        public bool CanHandle(Envelope<TRequest> request)
        {
            return _func.IsAlive;
        }

        public void Scatter(Envelope<TRequest> request, IGatherReceiver<TResponse> scatter)
        {
            GetResponses(Id, _func, request, scatter, Name);
        }

        public static IParticipant<TRequest, TResponse> Create(Func<TRequest, TResponse> func, string name)
        {
            return new ImmediateParticipant<TRequest, TResponse>(new StrongParticipantReference<TRequest, TResponse>(func), name);
        }

        public static void GetResponses(Guid id, IParticipantReference<TRequest, TResponse> func, Envelope<TRequest> request, IGatherReceiver<TResponse> scatter, string name)
        {
            try
            {
                var response = func.Invoke(request);
                scatter.AddResponse(id, ScatterResponse<TResponse>.Success(id, name, response));
            }
            catch (Exception e)
            {
                scatter.AddResponse(id, ScatterResponse<TResponse>.Error(id, name, e));
            }
        }
    }
}