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

        public static Task<TResponse> GetResponseAsync<TResponse>(this IRequest<TResponse> request)
        {
            return request.GetResponseAsync(new TimeSpan(0, 0, 10));
        }

        public static Task<bool> WaitForResponseAsync<TResponse>(this IRequest<TResponse> request)
        {
            return WaitForResponseAsync(request, new TimeSpan(0, 0, 10));
        }

        public static async Task<bool> WaitForResponseAsync<TResponse>(this IRequest<TResponse> request, TimeSpan timeout)
        {
            return await Task.Run(() => request.WaitForResponse(timeout)).ConfigureAwait(false);
        }

        public static async Task<TResponse> GetResponseAsync<TResponse>(this IRequest<TResponse> request, TimeSpan timeout)
        {
            return await WaitForResponseAsync(request, timeout).ContinueWith(okTask =>
            {
                bool ok = okTask.IsCompleted && !okTask.IsFaulted && okTask.Result;
                if (!ok)
                {
                    if (okTask.IsFaulted)
                        throw new Exception("Could not get response because of exception", okTask.Exception);
                    throw new Exception("Could not get response in alotted time");
                }
                request.ThrowExceptionIfError();
                return request.GetResponse();
            }).ConfigureAwait(false);
        }
    }
}