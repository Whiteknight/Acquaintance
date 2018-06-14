using System;

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
}