using System;
using System.Reflection;
using Acquaintance.Utility;

namespace Acquaintance.RequestResponse
{
    public class UntypedListenerBuilder
    {
        private readonly IReqResBus _messageBus;

        public UntypedListenerBuilder(IReqResBus messageBus)
        {
            _messageBus = messageBus;
        }

        private Type EnsureResponseType(Type requestType, Type responseType)
        {
            if (responseType != null)
                return responseType;
            var definedResponseType = requestType.GetRequestResponseType();
            if (definedResponseType != null)
                return definedResponseType;

            throw new Exception("No response type provided and one cannot be determined from metadata");
        }

        public IDisposable ListenUntyped(Type requestType, Type responseType, string topic, Func<object, object> func, bool useWeakReference = false)
        {
            Assert.ArgumentNotNull(requestType, nameof(requestType));
            Assert.ArgumentNotNull(func, nameof(func));

            responseType = EnsureResponseType(requestType, responseType);
            var method = GetMethod(nameof(ListenUntypedFuncInternal));
            method = method.MakeGenericMethod(requestType, responseType);
            return method.Invoke(this, new object[] { topic, func, useWeakReference }) as IDisposable;
        }

        public IDisposable ListenUntyped(Type requestType, Type responseType, string topic, object target, MethodInfo listener, bool useWeakReference = false)
        {
            Assert.ArgumentNotNull(requestType, nameof(requestType));
            Assert.ArgumentNotNull(target, nameof(target));
            Assert.ArgumentNotNull(listener, nameof(listener));

            responseType = EnsureResponseType(requestType, responseType);
            var method = GetMethod(nameof(ListenUntypedMethodInfoInternal));
            method = method.MakeGenericMethod(requestType, responseType);
            return method.Invoke(this, new object[] { topic, target, listener, useWeakReference }) as IDisposable;
        }

        public IDisposable ListenEnvelopeUntyped(Type requestType, Type responseType, string topic, object target, MethodInfo listener, bool useWeakReference = false)
        {
            Assert.ArgumentNotNull(requestType, nameof(requestType));
            Assert.ArgumentNotNull(target, nameof(target));
            Assert.ArgumentNotNull(listener, nameof(listener));

            responseType = EnsureResponseType(requestType, responseType);
            var method = GetMethod(nameof(ListenEnvelopeUntypedMethodInfoInternal));
            method = method.MakeGenericMethod(requestType, responseType);
            return method.Invoke(this, new object[] { topic, target, listener, useWeakReference }) as IDisposable;
        }

        private MethodInfo GetMethod(string name)
        {
            var method = GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod);
            if (method == null)
                throw new Exception($"Could not find method {GetType().Name}.{name}");
            return method;
        }

        private IDisposable ListenUntypedMethodInfoInternal<TRequest, TResponse>(string topic, object target, MethodInfo method, bool useWeakReference)
        {
            return _messageBus.Listen<TRequest, TResponse>(b => b
                .WithTopic(topic)
                .Invoke(p => (TResponse)method.Invoke(target, new object[] { p }), useWeakReference));
        }

        private IDisposable ListenUntypedFuncInternal<TRequest, TResponse>(string topic, Func<object, object> func, bool useWeakReference)
        {
            return _messageBus.Listen<TRequest, TResponse>(b => b
                .WithTopic(topic)
                .Invoke(p => (TResponse)func(p), useWeakReference));
        }

        private IDisposable ListenEnvelopeUntypedMethodInfoInternal<TRequest, TResponse>(string topic, object target, MethodInfo method, bool useWeakReference)
        {
            return _messageBus.Listen<TRequest, TResponse>(b => b
                .WithTopic(topic)
                .InvokeEnvelope(p => (TResponse)method.Invoke(target, new object[] { p }), useWeakReference));
        }
    }
}
