namespace Acquaintance.RabbitMq
{
    public class RabbitSenderOptions
    {
        public string QueueName { get; set; }
        public string RemoteTopic { get; set; }
        public int MessageExpirationMs { get; set; }
        public byte MessagePriority { get; set; } 
    }
}