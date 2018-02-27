using System;
using Acquaintance.Utility;

namespace Acquaintance.RequestResponse
{
    public class WrappedFunction<TRequest, TResponse>
    {
        public WrappedFunction(Func<TRequest, TResponse> function, IDisposable token, string topic)
        {
            Function = function;
            Token = token;
            Topic = topic;
        }

        public Func<TRequest, TResponse> Function { get; }
        public IDisposable Token { get; }
        public string Topic { get; }
    }

    public class RequestFuncWrapper<TRequest, TResponse>
    {
        public WrappedFunction<TRequest, TResponse> WrapFunction(IReqResBus messageBus, Func<TRequest, TResponse> func, Action<IThreadListenerBuilder<TRequest, TResponse>> build)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(func, nameof(func));

            var topic = Guid.NewGuid().ToString();
            var token = BuildListener(messageBus, func, build, topic);

            var newFunc = BuildFunction(messageBus, topic);
            return new WrappedFunction<TRequest, TResponse>(newFunc, token, topic);
        }

        private static Func<TRequest, TResponse> BuildFunction(IReqResBus messageBus, string topic)
        {
            TResponse NewFunc(TRequest req) => messageBus.RequestWait<TRequest, TResponse>(topic, req);
            return NewFunc;
        }

        private static IDisposable BuildListener(IReqResBus messageBus, Func<TRequest, TResponse> func, Action<IThreadListenerBuilder<TRequest, TResponse>> build, string topic)
        {
            var token = messageBus.Listen<TRequest, TResponse>(b =>
            {
                var c = b
                    .WithTopic(topic)
                    .Invoke(func);
                build?.Invoke(c);
            });
            return token;
        }
    }
}
