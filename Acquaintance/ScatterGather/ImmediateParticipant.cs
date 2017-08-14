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

        public ImmediateParticipant(IParticipantReference<TRequest, TResponse> func)
        {
            _func = func;
        }

        public Guid Id { get; set; }
        public bool ShouldStopParticipating => !_func.IsAlive;

        public bool CanHandle(TRequest request)
        {
            return _func.IsAlive;
        }

        public void Scatter(TRequest request, Scatter<TResponse> scatter)
        {
            GetResponses(Id, _func, request, scatter);
        }

        public static IParticipant<TRequest, TResponse> Create(Func<TRequest, TResponse> func)
        {
            return new ImmediateParticipant<TRequest, TResponse>(new StrongParticipantReference<TRequest, TResponse>(func));
        }

        public static void GetResponses(Guid id, IParticipantReference<TRequest, TResponse> func, TRequest request,  Scatter<TResponse> scatter)
        {
            try
            {
                var response = func.Invoke(request);
                scatter.AddResponse(id, response);
            }
            catch (Exception e)
            {
                scatter.AddError(id, e);
            }
        }
    }
}