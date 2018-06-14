using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Acquaintance.Logging;
using Acquaintance.RequestResponse;
using Acquaintance.Utility;

namespace Acquaintance.Scanning
{
    public class ListenerScanner
    {
        private readonly ILogger _logger;
        private readonly UntypedListenerBuilder _builder;

        public ListenerScanner(IReqResBus messageBus, ILogger logger)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));

            _logger = logger ?? messageBus.Logger;
            _builder = new UntypedListenerBuilder(messageBus);
        }

        public IDisposable DetectAndWireUp(object obj, bool useWeakReferences = false)
        {
            var tokens = DetectAndWireUpAll(obj, useWeakReferences);
            return new DisposableCollection(tokens);
        }

        public IEnumerable<IDisposable> DetectAndWireUpAll(object obj, bool useWeakReferences = false)
        {
            Assert.ArgumentNotNull(obj, nameof(obj));

            var type = obj.GetType();
            var methods = GetListenableMethods(type);
            return methods.Select(m => WireupMethod(obj, m, type, useWeakReferences));
        }

        private IDisposable WireupMethod(object obj, ListenableMethod method, Type type, bool useWeakReferences)
        {
            var methodInfo = GetQualifiedMethodInfo(method, type);
            var requestType = methodInfo.GetParameters()?.FirstOrDefault()?.ParameterType;
            var responseType = methodInfo.ReturnType;
            if (responseType == typeof(void))
            {
                _logger.Error($"Could not find valid response type for {type.Name}.{methodInfo.Name}");
                return null;
            }
            if (requestType == null)
            {
                _logger.Error($"Could not find valid request type for {type.Name}.{methodInfo.Name}");
                return null;
            }

            // If the method does not have a parameter, subscribe a trampoline to get to that method
            // TODO: Instead of a trampoline to MethodInfo.Invoke, can we compile an expression to do this?
            if (method.ParameterType == null)
                return _builder.ListenUntyped(requestType, responseType, method.Listener.Topic, x => method.Method.Invoke(obj, new object[0]));

            // If the parameter is Envelope<> setup the listener that way
            if (method.UsesEnvelope)
                return _builder.ListenEnvelopeUntyped(requestType, responseType, method.Listener.Topic, obj, method.Method, useWeakReferences);

            // Setup a normal listener without Envelope<T>
            if (method.ParameterType.IsAssignableFrom(requestType))
                return _builder.ListenUntyped(requestType, responseType, method.Listener.Topic, obj, method.Method, useWeakReferences);

            _logger.Error($"Could not add subscription {type.Name}.{methodInfo.Name} because parameter of type {method.ParameterType.Name} is not assignable from {requestType.Name}");
            return null;
        }

        private MethodInfo GetQualifiedMethodInfo(ListenableMethod method, Type type)
        {
            var methodInfo = method.Method;
            if (methodInfo == null)
            {
                _logger.Error("Null method cannot be used");
                return null;
            }

            if (methodInfo.IsGenericMethod)
                methodInfo = BuildGenericMethod(methodInfo, method.RequestType, method.ResponseType);
            if (methodInfo == null)
            {
                _logger.Error($"Could not find suitable method for {type.Name}.{method.Method.Name}. Maybe it is a generic method without suitable parameters?");
                return null;
            }

            return methodInfo;
        }

        private MethodInfo BuildGenericMethod(MethodInfo methodInfo, Type requestType, Type responseType)
        {
            var genericParams = methodInfo.GetGenericArguments();
            if (genericParams.Length == 0)
                return methodInfo;
            if (genericParams.Length != 2)
                return null;
            try
            {
                return methodInfo.MakeGenericMethod(requestType, responseType);
            }
            catch
            {
                return null;
            }
        }

        private class CandidateListenableMethod
        {
            public CandidateListenableMethod(MethodInfo method)
            {
                Method = method;
                Listeners = method.GetCustomAttributes(typeof(ListenerAttribute))
                    .OfType<ListenerAttribute>()
                    .ToArray();
                Parameters = method.GetParameters();
            }

            public MethodInfo Method { get; }
            public ListenerAttribute[] Listeners { get; }
            public ParameterInfo[] Parameters { get; }
        }

        private class ListenableMethod
        {
            public ListenableMethod(MethodInfo method, ListenerAttribute listener, Type requestType, Type responseType, Type parameterType, bool usesEnvelope)
            {
                Method = method;
                Listener = listener;
                RequestType = requestType;
                ResponseType = responseType;
                ParameterType = parameterType;
                UsesEnvelope = usesEnvelope;
            }

            public MethodInfo Method { get;  }
            public ListenerAttribute Listener { get; }
            public Type RequestType { get; }
            public Type ResponseType { get;  }
            public Type ParameterType { get;  }
            public bool UsesEnvelope { get; }
        }

        private IEnumerable<ListenableMethod> GetListenableMethods(Type type)
        {
            return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod)
                .Where(m => !m.IsAbstract && m.ReturnType != typeof(void))
                .Select(m => new CandidateListenableMethod(m))
                .SelectMany(c => GetListenableMethods(type, c));
        }

        private IEnumerable<ListenableMethod> GetListenableMethods(Type type, CandidateListenableMethod candidate)
        {
            if (candidate?.Listeners == null || !candidate.Listeners.Any())
                return Enumerable.Empty<ListenableMethod>();
            if (candidate.Parameters.Length > 1)
            {
                _logger.Error($"Could not add listener {type.Name}.{candidate.Method.Name} because it has too many parameters");
                return Enumerable.Empty<ListenableMethod>();
            }
            if (candidate.Method.ReturnType == typeof(void))
            {
                _logger.Error($"Could not add listener {type.Name}.{candidate.Method.Name} because it does not have a return value");
                return Enumerable.Empty<ListenableMethod>();
            }

            return candidate.Listeners
                .Select(l => GetListenableMethod(candidate.Method, l))
                .Where(m => m != null);
        }

        private ListenableMethod GetListenableMethod(MethodInfo method, ListenerAttribute listener)
        {
            Type requestType = listener.Request.UnwrapEnvelopeType();
            Type responseType = listener.Response.UnwrapEnvelopeType();

            if (method.IsGenericMethodDefinition)
            {
                var numTypeArgs = method.GetGenericArguments().Length;
                if (numTypeArgs > 2)
                {
                    _logger.Error($"Cannot subscribe method {method.Name} because method is generic and there is not enough information to construct it");
                    return null;
                }

                if (numTypeArgs == 2 && requestType != null && responseType != null)
                    method = method.MakeGenericMethod(requestType, responseType);
                if (numTypeArgs == 1 && (requestType ?? responseType) != null)
                {
                    var typeToUse = requestType ?? responseType;
                    _logger.Warn($"Method {method.Name} has one type argument. Assuming to use {typeToUse.Name}");
                    method = method.MakeGenericMethod(requestType);
                }
            }

            Type parameterType = method.GetParameters().FirstOrDefault()?.ParameterType;
            if (responseType != null && !responseType.IsAssignableFrom(method.ReturnType))
            {
                _logger.Error($"Conflicting response types. {responseType.Name} not assignable from {method.ReturnType.Name}");
                return null;
            }

            // If the request type implements IRequest<TResponse> we can get the response type from there
            if (responseType == null)
            {
                var definedResponseType = requestType.GetRequestResponseType();
                if (definedResponseType != null)
                    responseType = definedResponseType;
                else
                    responseType = method.ReturnType;
            }

            // Response MyMethod()
            // There is no parameter, so see if we have enough information to construct a trampoline
            if (parameterType == null)
            {
                // We have no type information at all, so we can't do anything
                if (requestType == null)
                {
                    _logger.Error($"Could not determine which channel to use because payload type is not provided and there is no parameter");
                    return null;
                }

                // This is not a fully-constructed type, so we can't determine a channel for it
                if (requestType.IsGenericType && !requestType.IsConstructedGenericType)
                {
                    _logger.Error($"Payload type {requestType.Name} is not fully-constructed and cannot be used to define a channel");
                    return null;
                }

                // Use the payload type from the attribute to create a trampoline to a method with no parameters
                return new ListenableMethod(method, listener, requestType, responseType, null, false);
            }

            // parameterType is the type of the raw parameter, which may include Envelope<>
            // parameterPayloadType is the type of the payload without Envelope<>

            bool isEnvelope = parameterType.IsEnvelopeType();
            Type parameterPayloadType = parameterType.UnwrapEnvelopeType();
            if (requestType == null)
                requestType = parameterPayloadType;

            // void MyMethod<T>(T payload) or void MyMethod<T>(Envelope<T> payload)
            // This is not a fully-constructed type, so we can't determine a channel for it
            if (requestType.IsGenericType && !requestType.IsConstructedGenericType)
            {
                _logger.Error($"Payload type {requestType.Name} is not fully-constructed and cannot be used");
                return null;
            }

            // Check that the type information between parameter and specified payload type are assignable
            if (!parameterPayloadType.IsAssignableFrom(requestType))
            {
                _logger.Error($"Cannot subscribe method {method.Name} because specified payload type {requestType.Name} is not assignable to parameter type {parameterPayloadType.Name}");
                return null;
            }

            return new ListenableMethod(method, listener, requestType, responseType, parameterType, isEnvelope);
        }
    }
}