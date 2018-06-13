using System;
using System.Reflection;
using Acquaintance.Utility;

namespace Acquaintance.PubSub
{
    public class UntypedSubscriptionBuilder
    {
        private readonly IPubSubBus _messageBus;

        public UntypedSubscriptionBuilder(IPubSubBus messageBus)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            _messageBus = messageBus;
        }

        public IDisposable SubscribeUntyped(Type payloadType, string[] topics, Action act, bool useWeakReference = false)
        {
            Assert.ArgumentNotNull(payloadType, nameof(payloadType));
            Assert.ArgumentNotNull(act, nameof(act));

            var method = GetMethod(nameof(SubscribeUntypedActionInternal));
            method = method.MakeGenericMethod(payloadType);
            return method.Invoke(this, new object[] { topics, act, useWeakReference }) as IDisposable;
        }

        public IDisposable SubscribeUntyped(Type payloadType, string[] topics, object target, MethodInfo subscriber, bool useWeakReference = false)
        {
            Assert.ArgumentNotNull(payloadType, nameof(payloadType));
            Assert.ArgumentNotNull(target, nameof(target));
            Assert.ArgumentNotNull(subscriber, nameof(subscriber));

            var method = GetMethod(nameof(SubscribeUntypedMethodInfoInternal));
            method = method.MakeGenericMethod(payloadType);
            return method.Invoke(this, new[] { topics, target, subscriber, useWeakReference }) as IDisposable;
        }

        public IDisposable SubscribeEnvelopeUntyped(Type payloadType, string[] topics, object target, MethodInfo subscriber, bool useWeakReference = false)
        {
            Assert.ArgumentNotNull(payloadType, nameof(payloadType));
            Assert.ArgumentNotNull(target, nameof(target));
            Assert.ArgumentNotNull(subscriber, nameof(subscriber));

            var method = GetMethod(nameof(SubscribeEnvelopeUntypedInternal));
            method = method.MakeGenericMethod(payloadType);
            return method.Invoke(this, new[] { topics, target, subscriber, useWeakReference }) as IDisposable;
        }

        private MethodInfo GetMethod(string name)
        {
            var method = GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod);
            if (method == null)
                throw new Exception($"Could not find method {GetType().Name}.{name}");
            return method;
        }

        private IDisposable SubscribeUntypedActionInternal<TPayload>(string[] topics, Action act, bool useWeakReference)
        {
            if (topics == null)
            {
                return _messageBus.Subscribe<TPayload>(b => b
                    .ForAllTopics()
                    .Invoke(p => act(), useWeakReference));
            }

            if (topics.Length == 0)
                topics = new[] { string.Empty };

            return _messageBus.Subscribe<TPayload>(b => b
                .WithTopic(topics)
                .Invoke(p => act(), useWeakReference));
        }

        private IDisposable SubscribeUntypedMethodInfoInternal<TPayload>(string[] topics, object target, MethodInfo subscriber, bool useWeakReference)
        {
            if (topics == null)
            {
                return _messageBus.Subscribe<TPayload>(b => b
                    .ForAllTopics()
                    .Invoke(p => subscriber.Invoke(target, new object[] { p }), useWeakReference));
            }

            if (topics.Length == 0)
                topics = new[] { string.Empty };

            return _messageBus.Subscribe<TPayload>(b => b
                .WithTopic(topics)
                .Invoke(p => subscriber.Invoke(target, new object[] { p }), useWeakReference));
        }

        private IDisposable SubscribeEnvelopeUntypedInternal<TPayload>(string[] topics, object target, MethodInfo subscriber, bool useWeakReference)
        {
            if (topics == null || topics.Length == 0)
            {
                return _messageBus.Subscribe<TPayload>(b => b
                    .WithDefaultTopic()
                    .InvokeEnvelope(e => subscriber.Invoke(target, new object[] { e }), useWeakReference));
            }

            return _messageBus.Subscribe<TPayload>(b => b
                .WithTopic(topics)
                .InvokeEnvelope(e => subscriber.Invoke(target, new object[] { e }), useWeakReference));
        }
    }
}
