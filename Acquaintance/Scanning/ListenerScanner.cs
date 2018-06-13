using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Acquaintance.Logging;
using Acquaintance.RequestResponse;
using Acquaintance.Utility;

namespace Acquaintance.Scanning
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ListenerAttribute : Attribute
    {
        public Type Request { get; set; }
        public Type Response { get; set; }
        public string Topic { get; set; }

        public ListenerAttribute(Type request, Type response)
        {
            Request = request;
            Response = response;
        }

        public ListenerAttribute(Type request, Type response, string topic)
        {
            Request = request;
            Response = response;
            Topic = topic;
        }
    }

    public class ListenerScanner
    {
        private readonly IReqResBus _messageBus;
        private readonly ILogger _logger;
        private readonly UntypedListenerBuilder _builder;

        public ListenerScanner(IReqResBus messageBus, ILogger logger)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));

            _messageBus = messageBus;
            _logger = logger ?? messageBus.Logger;
            _builder = new UntypedListenerBuilder(messageBus);
        }

        public IEnumerable<IDisposable> DetectAndWireUpListeners(object obj, bool useWeakReferences = false)
        {
            Assert.ArgumentNotNull(obj, nameof(obj));

            var type = obj.GetType();
            var methods = GetListenableMethods(type);
            return null;
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

            var envelopeType = typeof(Envelope<>).MakeGenericType(requestType);
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
            public ListenableMethod(MethodInfo method, ListenerAttribute listener)
            {
                Method = method;
                Listener = listener;
                RequestType = listener?.Request ?? method.GetParameters()?.FirstOrDefault()?.ParameterType;
                ResponseType = listener?.Response ?? method.ReturnType;
            }

            public MethodInfo Method { get;  }
            public ListenerAttribute Listener { get; }
            public Type RequestType { get; }
            public Type ResponseType { get;  }
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

            return candidate.Listeners.Select(l => new ListenableMethod(candidate.Method, l));
        }
    }
}