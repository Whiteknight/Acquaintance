using System;
using System.Threading;
using Acquaintance;
using Acquaintance.Logging;
using Acquaintance.RabbitMq;

namespace RabbitTester
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Setting up MessageBus");
            var messageBus = new MessageBusBuilder().UseLogger(new DelegateLogger(Console.WriteLine)).Build();
            var t1 = messageBus.InitializeRabbitMq("host=vboxguest.asinetwork.local;username=guest;password=guest");

            // Messages from local, with topic "send" are sent to rabbit
            var t2 = messageBus.Subscribe<MyTestPayload>(b => b
                .WithTopic("send")
                .ForwardToRabbitMq(r => r.UseDefaultQueue())
                .OnWorker()
            );

            messageBus.Subscribe<MyTestPayload>(b => b
                .WithTopic("send")
                .Invoke(s => Console.WriteLine("Sending: " + s.Text)));

            // Subscript to "send" topic on rabbit, and forward those messages locally on the "received" topic
            var t3 = messageBus.PullRabbitMqToLocal<MyTestPayload>(r => r
                .ForAllRemoteTopics()
                .ReceiveDefaultFormat()
                .ForwardToLocalTopic("received")
                .UseSharedQueueName()
                .AutoExpireQueue());

            // Messages from rabbit, with topic "received" get printed on the console
            var t4 = messageBus.Subscribe<MyTestPayload>(b => b
                .WithTopic("received")
                .InvokeEnvelope(m =>
                {
                    Console.WriteLine("Received: " + m.Payload.Text);
                }));

            //Console.WriteLine("Sending message (should print 'whatever' if successful)");
            for (int i = 0; i < 100; i++)
            {
                messageBus.Publish("send", new MyTestPayload
                {
                    Text = "test message " + i
                });
                Thread.Sleep(1000);
            }

            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
            messageBus.Dispose();
            Environment.Exit(0);
        }
    }

    public class MyTestPayload
    {
        public string Text { get; set; }
    }
}
