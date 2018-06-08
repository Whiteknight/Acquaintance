using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Acquaintance.Logging;
using Acquaintance.Utility;

namespace Acquaintance.PubSub
{
    public class SubscriptionScanner
    {
        private readonly IPubSubBus _messageBus;
        private readonly ILogger _logger;

        public SubscriptionScanner(IPubSubBus messageBus, ILogger logger)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(logger, nameof(logger));

            _messageBus = messageBus;
            _logger = logger;
        }

        public IDisposable AutoSubscribe(object obj, bool useWeakReference = false)
        {
            Assert.ArgumentNotNull(obj, nameof(obj));

            var type = obj.GetType();
            var methods = GetTypeMethods(type);
            var tokens = new DisposableCollection();
            foreach (var method in methods)
            {
                var token = SubscribeMethod(obj, method, type, useWeakReference);
                if (token != null)
                    tokens.Add(token);
            }
            return tokens;
        }

        private IDisposable SubscribeMethod(object obj, SubscribableMethod method, Type type, bool useWeakReference)
        {
            Type payloadType = method.Subscription.Type;
            if (payloadType == null && method.Parameter != null)
                payloadType = method.Parameter.ParameterType;

            if (payloadType == null)
            {
                _logger.Error($"Could not determine payload type for subscription {type.Name}.{method.Method.Name}");
                return null;
            }

            var parameter = method.Parameter;
            var methodInfo = method.Method;
            if (methodInfo.IsGenericMethod)
            {
                methodInfo = BuildGenericMethod(type, methodInfo, payloadType);
                parameter = methodInfo.GetParameters().FirstOrDefault();
            }

            if (methodInfo == null)
            {
                _logger.Error($"Could not find suitable method for {type.Name}.{method.Method.Name}. Maybe it is a generic method without suitable parameters?");
                return null;
            }

            // If the method does not have a parameter, subscribe a trampoline to get to that method
            if (parameter == null)
            {
                return _messageBus.SubscribeUntyped(payloadType, method.Subscription.Topics, () => methodInfo.Invoke(obj, new object[0]), useWeakReference);
            }

            // If the method has a parameter, and it's an Envelope<> type, subscribe it that way
            var envelopeType = typeof(Envelope<>).MakeGenericType(payloadType);
            if (parameter.ParameterType.IsAssignableFrom(envelopeType))
                return _messageBus.SubscribeEnvelopeUntyped(payloadType, method.Subscription.Topics, obj, methodInfo, useWeakReference);

            // If the parameter type is assignable from the payload type, subscribe it that way
            if (parameter.ParameterType.IsAssignableFrom(payloadType))
                return _messageBus.SubscribeUntyped(payloadType, method.Subscription.Topics, obj, methodInfo, useWeakReference);

            // We can't match the type, log an error
            _logger.Error($"Could not add subscription {type.Name}.{methodInfo.Name} because parameter of type {parameter.ParameterType.Name} is not assignable from {payloadType.Name}");
            return null;
        }

        private MethodInfo BuildGenericMethod(Type type, MethodInfo methodInfo, Type payloadType)
        {
            try
            {
                return methodInfo.MakeGenericMethod(payloadType);
            }
            catch
            {
                return null;
            }
        }


        private class CandidateMethod
        {
            public MethodInfo Method { get; set; }
            public SubscriptionAttribute[] Subscriptions { get; set; }
            public ParameterInfo[] Parameters { get; set; }
        }

        private class SubscribableMethod
        {
            public MethodInfo Method { get; set; }
            public SubscriptionAttribute Subscription { get; set; }
            public ParameterInfo Parameter { get; set; }
        }

        private IEnumerable<SubscribableMethod> GetTypeMethods(Type type)
        {
            return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod)
                .Where(m => m.ReturnType == typeof(void))
                .Where(m => !m.IsAbstract)
                .Select(m => new CandidateMethod
                {
                    Method = m,
                    Subscriptions = m.GetCustomAttributes(typeof(SubscriptionAttribute))
                        .OfType<SubscriptionAttribute>()
                        .ToArray(),
                    Parameters = m.GetParameters()
                })
                .SelectMany(c => GetSubscribableMethods(type, c));
        }

        private IEnumerable<SubscribableMethod> GetSubscribableMethods(Type type, CandidateMethod candidate)
        {
            if (candidate?.Subscriptions == null || !candidate.Subscriptions.Any())
                return Enumerable.Empty<SubscribableMethod>();
            if (candidate.Parameters.Length > 1)
            {
                _logger.Error($"Could not add subscription {type.Name}.{candidate.Method.Name} because it has too many parameters");
                return Enumerable.Empty<SubscribableMethod>();
            }

            return candidate.Subscriptions.Select(s => new SubscribableMethod
            {
                Method = candidate.Method,
                Parameter = candidate.Parameters.FirstOrDefault(),
                Subscription = s
            });
        }
    }
}
