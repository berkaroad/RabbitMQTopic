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
            var producer = new Producer(new ProducerSettings
            {
                AmqpUri = new Uri("amqp://demo:123456@localhost/test"),
                ClientName = "ProducerApp"
            });

            var random = new Random();
            var sendCount = 10000;
            var sendTaskList = new List<Task>();
            var watch = Stopwatch.StartNew();
            producer.Start();
            for (var i = 0; i < sendCount; i++)
            {
                var messageId = Guid.NewGuid().ToString();
                var routingKey = Guid.NewGuid().ToString();
                var body = System.Text.Encoding.UTF8.GetBytes($"{i} delayed message {messageId}");
                var topicMessage = new TopicMessage("CommandTopic", 4, 1, body, "text/json", tag: "System.String");
                sendTaskList.Add(producer.SendMessageAsync(topicMessage, routingKey, messageId));
            }
            Task.WaitAll(sendTaskList.ToArray());
            var spentTime = watch.ElapsedMilliseconds;
            Console.WriteLine(string.Empty);
            Console.WriteLine("Send message completed, time spent: {0}ms, throughput: {1} transactions per second.", spentTime, sendCount * 1000 / spentTime);
            Thread.Sleep(500);

            producer.Shutdown();

            var consumeCount = 0;
            var consumer = new Consumer(new ConsumerSettings
            {
                AmqpUri = new Uri("amqp://demo:123456@localhost/test"),
                ClientName = "ConsumerApp",
                PrefetchCount = 1000,
                GroupName = "Group1"
            });
            consumer.OnMessageReceived += (sender, e) =>
            {
                Interlocked.Increment(ref consumeCount);
                e.Context.Ack();
            };

            consumer.Subscribe("CommandTopic", 4);
            watch.Restart();
            consumer.Start();
            while (consumeCount >= sendCount)
            {
                Thread.Sleep(1);
                break;
            }

            spentTime = watch.ElapsedMilliseconds;
            Thread.Sleep(500);
            Console.WriteLine(string.Empty);
            Console.WriteLine("Consume message completed, time spent: {0}ms, throughput: {1} transactions per second.", spentTime, consumeCount * 1000 / spentTime);

            Thread.Sleep(1000);
            consumer.Shutdown();
        }
    }
}
