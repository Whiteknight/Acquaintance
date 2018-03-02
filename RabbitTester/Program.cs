using System;
using Acquaintance;
using Acquaintance.RabbitMq;

namespace RabbitTester
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Setting up MessageBus");
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

            Console.WriteLine("Sending message (should print 'whatever' if successful)");
            messageBus.Publish("send", new MyTestPayload
            {
                Text = "whatever"
            });
            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
            messageBus.Dispose();
            //Environment.Exit(0);
        }
    }

    public class MyTestPayload
    {
        public string Text { get; set; }
    }
}
