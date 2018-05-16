using System;

namespace Acquaintance.RabbitMq
{
    public static class RabbitUtility
    {
        public static string CreateSubscriberId()
        {
            var id = Guid.NewGuid().ToString();
            return id;
        }
    }
}