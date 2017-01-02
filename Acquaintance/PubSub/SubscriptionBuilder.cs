using System;

namespace Acquaintance.PubSub
{
    public class SubscriptionBuilder<TPayload>
    {
        private readonly SubscribeOptions _options;
        private readonly Action<TPayload> _subscriber;
        private Func<TPayload, bool> _filter;
        private string _name;
        private readonly ISubscribable _messageBus;

        public SubscriptionBuilder(ISubscribable messageBus, Action<TPayload> subscriber)
        {
            _subscriber = subscriber;
            _options = new SubscribeOptions();
            _messageBus = messageBus;
        }

        public IDisposable Subscribe()
        {
            return _messageBus.Subscribe<TPayload>(_name, _subscriber, _filter, _options);
        }

        public SubscriptionBuilder<TPayload> WithChannelName(string name)
        {
            _name = name;
            return this;
        }

        public SubscriptionBuilder<TPayload> WithFilter(Func<TPayload, bool> filter)
        {
            _filter = filter;
            return this;
        }

        public SubscriptionBuilder<TPayload> OnWorkerThread()
        {
            _options.DispatchType = Threading.DispatchThreadType.AnyWorkerThread;
            return this;
        }

        public SubscriptionBuilder<TPayload> Immediate()
        {
            _options.DispatchType = Threading.DispatchThreadType.Immediate;
            return this;
        }

        public SubscriptionBuilder<TPayload> OnThread(int threadId)
        {
            _options.DispatchType = Threading.DispatchThreadType.SpecificThread;
            _options.ThreadId = threadId;
            return this;
        }

        public SubscriptionBuilder<TPayload> OnThreadPool()
        {
            _options.DispatchType = Threading.DispatchThreadType.ThreadpoolThread;
            return this;
        }
    }
}
