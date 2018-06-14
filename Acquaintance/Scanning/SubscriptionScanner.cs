using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Acquaintance.Logging;
using Acquaintance.PubSub;
using Acquaintance.Utility;

namespace Acquaintance.Scanning
{
    public class SubscriptionScanner
    {
        private readonly ILogger _logger;
        private readonly UntypedSubscriptionBuilder _builder;

        public SubscriptionScanner(IPubSubBus messageBus, ILogger logger)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));

            _logger = logger ?? messageBus.Logger;
            _builder = new UntypedSubscriptionBuilder(messageBus);
        }

        public IDisposable DetectAndWireUp(object obj, bool useWeakReferences = false)
        {
            var tokens = DetectAndWireUpAll(obj, useWeakReferences);
            return new DisposableCollection(tokens);
        }

        public IEnumerable<IDisposable> DetectAndWireUpAll(object obj, bool useWeakReference = false)
        {
            Assert.ArgumentNotNull(obj, nameof(obj));

            var type = obj.GetType();
            var methods = GetSubscribeableMethods(type).ToList();
            return methods.Select(m => WireupMethod(obj, m, type, useWeakReference));
        }

        private IDisposable WireupMethod(object obj, SubscribeableMethod method, Type type, bool useWeakReference)
        {
            if (method.PayloadType == null)
            {
                _logger.Error($"Could not determine payload type for subscription {type.Name}.{method.Method.Name}");
                return null;
            }

            var methodInfo = method.Method;

            // If the method does not have a parameter, subscribe a trampoline to get to that method
            // TODO: Instead of a trampoline to MethodInfo.Invoke, can we compile an expression to do this?
            if (method.ParameterType == null)
                return _builder.SubscribeUntyped(method.PayloadType, method.Subscription.Topics, () => methodInfo.Invoke(obj, new object[0]), useWeakReference);

            // If the method has a parameter, and it's an Envelope<> type, subscribe it that way
            if (method.IsEnvelopeType)
                return _builder.SubscribeEnvelopeUntyped(method.PayloadType, method.Subscription.Topics, obj, methodInfo, useWeakReference);

            // If the parameter type is assignable from the payload type, subscribe it that way
            if (method.ParameterType.IsAssignableFrom(method.PayloadType))
                return _builder.SubscribeUntyped(method.PayloadType, method.Subscription.Topics, obj, methodInfo, useWeakReference);

            // We can't match the type, log an error
            _logger.Error($"Could not add subscription {type.Name}.{methodInfo.Name} because parameter of type {method.ParameterType.Name} is not assignable from {method.PayloadType.Name}");
            return null;
        }

        private class CandidateSubscribableMethod
        {
            public CandidateSubscribableMethod(MethodInfo method)
            {
                Method = method;
                Subscriptions = method.GetCustomAttributes(typeof(SubscriptionAttribute))
                    .OfType<SubscriptionAttribute>()
                    .ToArray();
                Parameters = method.GetParameters();
            }

            public MethodInfo Method { get;  }
            public SubscriptionAttribute[] Subscriptions { get;  }
            public ParameterInfo[] Parameters { get; }
        }

        private class SubscribeableMethod
        {
            public SubscribeableMethod(MethodInfo method, SubscriptionAttribute subscription, Type payloadType, Type parameterType, bool isEnvelope)
            {
                Method = method;
                Subscription = subscription;
                PayloadType = payloadType;
                ParameterType = parameterType;
                IsEnvelopeType = isEnvelope;
            }

            public MethodInfo Method { get; }
            public SubscriptionAttribute Subscription { get; }
            public Type PayloadType { get; }
            public Type ParameterType { get; }
            public bool IsEnvelopeType { get; }
        }

        private IEnumerable<SubscribeableMethod> GetSubscribeableMethods(Type type)
        {
            return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod)
                .Where(m => m.ReturnType == typeof(void))
                .Where(m => !m.IsAbstract)
                .Select(m => new CandidateSubscribableMethod(m))
                .SelectMany(c => GetSubscribeableMethods(type, c));
        }

        private IEnumerable<SubscribeableMethod> GetSubscribeableMethods(Type type, CandidateSubscribableMethod candidateSubscribable)
        {
            if (candidateSubscribable?.Subscriptions == null || !candidateSubscribable.Subscriptions.Any())
                return Enumerable.Empty<SubscribeableMethod>();
            if (candidateSubscribable.Parameters.Length > 1)
            {
                _logger.Error($"Could not add subscription {type.Name}.{candidateSubscribable.Method.Name} because it has too many parameters");
                return Enumerable.Empty<SubscribeableMethod>();
            }

            return candidateSubscribable.Subscriptions
                .Select(s => GetSubscribeableMethod(candidateSubscribable.Method, s))
                .Where(m => m != null);
        }

        private SubscribeableMethod GetSubscribeableMethod(MethodInfo method, SubscriptionAttribute subscription)
        {
            Type payloadType = subscription.PayloadType.UnwrapEnvelopeType();

            // void MyMethod<T>(T) or void MyMethod<T>() or void MyMethod<T>(Envelope<T>)
            // If the method is generic, we can try to fill in the type parameters with the value from the attribute
            if (method.IsGenericMethodDefinition)
            { 
                if (method.GetGenericArguments().Length != 1 || payloadType == null)
                {
                    _logger.Error($"Cannot subscribe method {method.Name} because method is generic and there is not enough information to construct it");
                    return null;
                }
                method = method.MakeGenericMethod(payloadType);
            }

            Type parameterType = method.GetParameters().FirstOrDefault()?.ParameterType;

            // void MyMethod()
            // There is no parameter, so see if we have enough information to construct a trampoline
            if (parameterType == null)
            {
                // We have no type information at all, so we can't do anything
                if (payloadType == null)
                {
                    _logger.Error($"Could not determine which channel to use because payload type is not provided and there is no parameter");
                    return null;
                }

                // This is not a fully-constructed type, so we can't determine a channel for it
                if (payloadType.IsGenericType && !payloadType.IsConstructedGenericType)
                {
                    _logger.Error($"Payload type {payloadType.Name} is not fully-constructed and cannot be used to define a channel");
                    return null;
                }

                // Use the payload type from the attribute to create a trampoline to a method with no parameters
                return new SubscribeableMethod(method, subscription, payloadType, null, false);
            }

            // parameterType is the type of the raw parameter, which may include Envelope<>
            // parameterPayloadType is the type of the payload without Envelope<>

            bool isEnvelope = parameterType.IsEnvelopeType();
            Type parameterPayloadType = parameterType.UnwrapEnvelopeType();

            if (payloadType == null)
                payloadType = parameterPayloadType;

            // void MyMethod<T>(T payload) or void MyMethod<T>(Envelope<T> payload)
            // This is not a fully-constructed type, so we can't determine a channel for it
            if (payloadType.IsGenericType && !payloadType.IsConstructedGenericType)
            {
                _logger.Error($"Payload type {payloadType.Name} is not fully-constructed and cannot be used");
                return null;
            }

            // Check that the type information between parameter and specified payload type are assignable
            if (!parameterPayloadType.IsAssignableFrom(payloadType))
            {
                _logger.Error($"Cannot subscribe method {method.Name} because specified payload type {payloadType.Name} is not assignable to parameter type {parameterPayloadType.Name}");
                return null;
            }

            return new SubscribeableMethod(method, subscription, payloadType, parameterType, isEnvelope);
        }
    }
}
