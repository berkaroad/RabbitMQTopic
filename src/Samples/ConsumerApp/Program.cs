using System;
using System.Threading;
using RabbitMQTopic;

namespace ConsumerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var consumer1 = new Consumer(new ConsumerSettings
            {
                AmqpUri = new Uri("amqp://demo:123456@localhost/test"),
                ClientName = "Consumer1App",
                PrefetchCount = 10,
                GroupName = "Group1",
                ConsumerCount = 2,
                ConsumerSequence = 1 // 将消费队列 0,2
            });
            consumer1.OnMessageReceived += (sender, e) =>
            {
                Console.WriteLine($"consumer1:{System.Text.Encoding.UTF8.GetString(e.Context.GetBody())}");
                e.Context.Ack();
            };
            var consumer2 = new Consumer(new ConsumerSettings
            {
                AmqpUri = new Uri("amqp://demo:123456@localhost/test"),
                ClientName = "Consumer2App",
                Mode = ConsumeMode.Pull,
                GroupName = "Group1",
                ConsumerCount = 2,
                ConsumerSequence = 2 // 将消费队列 1,3
            });
            consumer2.OnMessageReceived += (sender, e) =>
            {
                Console.WriteLine($"consumer2:{System.Text.Encoding.UTF8.GetString(e.Context.GetBody())}");
                e.Context.Ack();
            };

            consumer1.Subscribe("CommandTopic", 4);
            consumer2.Subscribe("CommandTopic", 4);

            consumer1.Start();
            consumer2.Start();
            Console.WriteLine("Consumer started!");

            Thread.Sleep(1000);
            consumer1.Shutdown();
            Console.WriteLine("Consumer1 shutdown!");
            consumer2.Shutdown();
            Console.WriteLine("Consumer2 shutdown!");
        }
    }
}
