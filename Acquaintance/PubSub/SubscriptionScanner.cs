using System;
using System.Linq;
using System.Reflection;
using Acquaintance.Utility;

namespace Acquaintance.PubSub
{
    public class SubscriptionScanner
    {
        // TODO: Clean this up and break it into smaller methods
        public IDisposable AutoSubscribe(IPubSubBus messageBus, object obj)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(obj, nameof(obj));

            var type = obj.GetType();
            var tokens = new DisposableCollection();
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod))
            {
                if (method.ReturnType != typeof(void))
                    continue;
                if (method.IsGenericMethod || method.IsAbstract)
                    continue;

                var attrs = method.GetCustomAttributes(typeof(SubscriptionAttribute))
                    .OfType<SubscriptionAttribute>()
                    .ToArray();
                if (attrs.Length == 0)
                    continue;

                var parameters = method.GetParameters();

                // TODO: Log this case
                if (parameters.Length != 1)
                    continue;

                foreach (var attr in attrs)
                {
                    Type payloadType = attr.Type;
                    if (attr.Type != null)
                    {
                        if (parameters[0].ParameterType.IsAssignableFrom(attr.Type))
                        {
                            var token = messageBus.SubscribeUntyped(attr.Type, attr.Topics, obj, method);
                            tokens.Add(token);
                            continue;
                        }
                    }
                    else
                    {
                        payloadType = parameters[0].ParameterType;
                    }


                    var envelopeType = typeof(Envelope<>).MakeGenericType(payloadType);
                    if (parameters[0].ParameterType.IsAssignableFrom(envelopeType))
                    {
                        var token = messageBus.SubscribeEnvelopeUntyped(payloadType, attr.Topics, obj, method);
                        tokens.Add(token);
                        continue;
                    }
                }
            }
            return tokens;
        }
    }
}
