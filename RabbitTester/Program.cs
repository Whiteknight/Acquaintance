using System;
using Acquaintance;
using Acquaintance.RabbitMq;

namespace RabbitTester
{
    class Program
    {
        static void Main(string[] args)
        {
            var messageBus = new MessageBus();
            messageBus.InitializeRabbitMq("host=localhost;username=guest;password=guest");

            messageBus.ForwardLocalToRabbit<MyTestPayload>("send");
            messageBus.ForwardRabbitToLocal<MyTestPayload>("send");
            messageBus.Subscribe<MyTestPayload>(b => b
                .WithTopic("send")
                .InvokeEnvelope(m =>
                {
                    Console.WriteLine(m.Payload.Text);
                }));

            messageBus.Publish("send", new MyTestPayload
            {
                Text = "whatever"
            });
            Console.ReadKey();
        }
    }

    public class MyTestPayload
    {
        public string Text { get; set; }
    }
}
