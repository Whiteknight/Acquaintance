using System.Collections.Generic;

namespace Acquaintance
{
    public class Conversation<TRequest, TResponse>
    {
        public Conversation(TRequest request, IReadOnlyList<TResponse> responses)
        {
            Request = request;
            Responses = responses;
        }

        public TRequest Request { get; private set; }
        public IReadOnlyList<TResponse> Responses { get; private set; }
    }
}
