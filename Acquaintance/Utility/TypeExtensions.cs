using System;
using System.Linq;
using Acquaintance.RequestResponse;

namespace Acquaintance.Utility
{
    public static class TypeExtensions
    {
        public static bool IsEnvelopeType(this Type type)
        {
            return type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Envelope<>);
        }

        public static Type UnwrapEnvelopeType(this Type type)
        {
            if (type == null)
                return null;
            if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Envelope<>))
                return type.GetGenericArguments().First();
            return type;
        }

        public static Type GetRequestResponseType(this Type type)
        {
            var requestInterfaceType = type.GetInterfaces().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IRequest<>));
            if (requestInterfaceType == null)
                return null;
            return requestInterfaceType.GetGenericArguments().First();
        }
    }
}
