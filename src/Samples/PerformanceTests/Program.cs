using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQTopic;

namespace PerformanceTests
{
    class Program
    {
        static void Main(string[] args)
        {
            PerfTest(ConsumeMode.Push);
            Thread.Sleep(1000);
            PerfTest(ConsumeMode.Pull);
        }

        static void PerfTest(ConsumeMode consumeMode)
        {
            var random = new Random();
            var messageCount = 100000;
            var bashSize = 1000;
            var sendTaskList = new List<Task>();
            var spentTime = 0L;

            var producer = new Producer(new ProducerSettings
            {
                AmqpUri = new Uri("amqp://demo:123456@localhost/test"),
                ClientName = "ProducerApp",
                MaxChannelPoolSize = bashSize
            });
            producer.RegisterTopic("CommandTopic", 4).Start();


            var watch = Stopwatch.StartNew();
            for (var i = 0; i < messageCount; i++)
            {
                var messageId = Guid.NewGuid().ToString();
                var routingKey = Guid.NewGuid().ToString();
                var body = System.Text.Encoding.UTF8.GetBytes($"{i} message {messageId}");
                var topicMessage = new Message("CommandTopic", 1, body, "text/json", tag: "System.String");
                sendTaskList.Add(producer.SendMessageAsync(topicMessage, routingKey));
                if ((i + 1) % bashSize == 0)
                {
                    Task.WaitAll(sendTaskList.ToArray());
                    sendTaskList.Clear();
                }
            }
            Task.WaitAll(sendTaskList.ToArray());
            sendTaskList.Clear();
            spentTime = watch.ElapsedMilliseconds;
            Console.WriteLine(string.Empty);
            Console.WriteLine($"Send message completed, time spent: {spentTime}ms, message count: {messageCount}, throughput: {messageCount * 1000 / spentTime}tps.");
            producer.Shutdown();

            var consumer = new Consumer(new ConsumerSettings
            {
                AmqpUri = new Uri("amqp://demo:123456@localhost/test"),
                ClientName = "ConsumerApp",
                Mode = consumeMode,
                PrefetchCount = 1000,
                GroupName = "Group1"
            });
            var consumeCount = 0;
            consumer.OnMessageReceived += (sender, e) =>
            {
                Interlocked.Increment(ref consumeCount);
                e.Context.Ack();
            };

            consumer.Subscribe("CommandTopic", 4);
            Thread.Sleep(500);
            watch.Restart();
            consumer.Start();
            while (consumeCount < messageCount)
            {
                Thread.Sleep(1);
            }
            spentTime = watch.ElapsedMilliseconds;
            Thread.Sleep(100);
            Console.WriteLine(string.Empty);
            Console.WriteLine($"Consume message by {consumeMode} completed, time spent: {spentTime}ms, message count: {consumeCount}, throughput: {consumeCount * 1000 / spentTime}tps.");

            Thread.Sleep(3000);
            consumer.Shutdown();
        }
    }
}
