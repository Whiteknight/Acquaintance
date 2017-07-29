using System;

namespace Acquaintance.PubSub
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class SubscriptionAttribute : Attribute
    {
        public Type Type { get; set; }
        public string[] Topics { get; set; }

        public SubscriptionAttribute(Type type)
        {
            Type = type;
        }

        public SubscriptionAttribute(Type type, string[] topics)
        {
            Type = type;
            Topics = topics;
        }
    }
}
