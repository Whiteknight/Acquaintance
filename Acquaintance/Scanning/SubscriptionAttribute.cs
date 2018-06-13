using System;

namespace Acquaintance.Scanning
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class SubscriptionAttribute : Attribute
    {
        public Type PayloadType { get; set; }
        // TODO: Option to subscribe to all topics?
        public string[] Topics { get; set; }

        public SubscriptionAttribute(Type payloadType)
        {
            PayloadType = payloadType;
        }

        public SubscriptionAttribute(Type payloadType, string[] topics)
        {
            PayloadType = payloadType;
            Topics = topics;
        }
    }
}
