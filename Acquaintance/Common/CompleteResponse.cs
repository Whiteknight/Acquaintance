using System;

namespace Acquaintance.Common
{
    public class CompleteResponse<T>
    {
        public CompleteResponse(T response, Exception errorInformation, bool completed = true)
        {
            Response = response;
            ErrorInformation = errorInformation;
            Completed = completed;
            Success = errorInformation == null;
        }

        public T Response { get; private set; }
        public bool Success { get; private set; }
        public Exception ErrorInformation { get; }
        public bool Completed { get; set; }

        public void ThrowExceptionIfPresent()
        {
            if (ErrorInformation != null)
                throw ErrorInformation;
        }
    }

    public class CompleteGather<T>
    {
        public CompleteGather(T[] responses, Exception errorInformation, bool completed = true)
        {
            Responses = responses;
            ErrorInformation = errorInformation;
            Completed = completed;
            Success = errorInformation == null;
        }

        public T[] Responses { get; private set; }
        public bool Success { get; private set; }
        public Exception ErrorInformation { get; }
        public bool Completed { get; set; }

        public void ThrowExceptionIfPresent()
        {
            if (ErrorInformation != null)
                throw ErrorInformation;
        }
    }
}
