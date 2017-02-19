using System;

namespace Acquaintance.PubSub
{
    public class WrappedAction<TPayload>
    {
        public WrappedAction(Action<TPayload> action, IDisposable token, string topic)
        {
            Action = action;
            Token = token;
            Topic = topic;
        }

        public Action<TPayload> Action { get; }
        public IDisposable Token { get; }
        public string Topic { get; }
    }
}
