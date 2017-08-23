using System;
using System.Threading.Tasks;

namespace Acquaintance.RequestResponse
{
    public static class RequestExtensions
    {
        public static TResponse GetResponseOrWait<TResponse>(this IRequest<TResponse> request, TimeSpan timeout)
        {
            request.WaitForResponse(timeout);
            request.ThrowExceptionIfError();
            return request.GetResponse();
        }

        public static TResponse GetResponseOrWait<TResponse>(this IRequest<TResponse> request)
        {
            return GetResponseOrWait(request, new TimeSpan(0, 0, 10));
        }

        public static async Task<TResponse> ToTask<TResponse>(this IRequest<TResponse> request)
        {
            return await Task
                .Run(() =>
                {
                    request.WaitForResponse();
                    request.ThrowExceptionIfError();
                    return request.GetResponse();
                })
                .ConfigureAwait(false);
        }
    }
}