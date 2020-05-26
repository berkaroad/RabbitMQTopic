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
                ClientName = "ProducerApp",
                MaxChannelIdleDuration = 2
            });
            producer.RegisterTopic("CommandTopic", 4).Start();

            var random = new Random();
            var messageCount = 100000;
            var sendTaskList = new List<Task>();
            var spentTime = 0L;

            var watch = Stopwatch.StartNew();
            for (var i = 0; i < messageCount; i++)
            {
                var messageId = Guid.NewGuid().ToString();
                var routingKey = Guid.NewGuid().ToString();
                var body = System.Text.Encoding.UTF8.GetBytes($"{i} message {messageId}");
                var topicMessage = new Message("CommandTopic", 1, body, "text/json", tag: "System.String");
                sendTaskList.Add(producer.SendMessageAsync(topicMessage, routingKey));
            }
            Task.WaitAll(sendTaskList.ToArray());
            sendTaskList.Clear();
            spentTime = watch.ElapsedMilliseconds;
            Console.WriteLine(string.Empty);
            Console.WriteLine($"Send message completed, time spent: {spentTime}ms, message count: {messageCount}, throughput: {messageCount * 1000 / spentTime}tps.");
            Thread.Sleep(3000);

            producer.Shutdown();

            var consumer = new Consumer(new ConsumerSettings
            {
                AmqpUri = new Uri("amqp://demo:123456@localhost/test"),
                ClientName = "ConsumerApp",
                Mode = ConsumeMode.Push,
                PrefetchCount = 1000,
                GroupName = "Group1"
            });
            var consumeCount1 = 0;
            consumer.OnMessageReceived += (sender, e) =>
            {
                Interlocked.Increment(ref consumeCount1);
                e.Context.Ack();
            };

            consumer.Subscribe("CommandTopic", 4);
            Thread.Sleep(500);
            watch.Restart();
            consumer.Start();
            while (consumeCount1 < messageCount / 2)
            {
                Thread.Sleep(1);
            }
            spentTime = watch.ElapsedMilliseconds;
            Thread.Sleep(100);
            consumer.Shutdown();
            Console.WriteLine(string.Empty);
            Console.WriteLine($"Consume message by push completed, time spent: {spentTime}ms, message count: {consumeCount1}, throughput: {consumeCount1 * 1000 / spentTime}tps.");


            consumer = new Consumer(new ConsumerSettings
            {
                AmqpUri = new Uri("amqp://demo:123456@localhost/test"),
                ClientName = "ConsumerApp",
                Mode = ConsumeMode.Pull,
                PrefetchCount = 1000,
                GroupName = "Group1"
            });
            var consumeCount2 = 0;
            consumer.OnMessageReceived += (sender, e) =>
            {
                Interlocked.Increment(ref consumeCount2);
                e.Context.Ack();
            };

            consumer.Subscribe("CommandTopic", 4);
            Thread.Sleep(500);
            watch.Restart();
            consumer.Start();
            while (consumeCount1 + consumeCount2 < messageCount)
            {
                Thread.Sleep(1);
            }
            spentTime = watch.ElapsedMilliseconds;
            Console.WriteLine(string.Empty);
            Console.WriteLine($"Consume message by pull completed, time spent: {spentTime}ms, message count: {consumeCount2}, throughput: {consumeCount2 * 1000 / spentTime}tps.");

            Thread.Sleep(1000);
            consumer.Shutdown();
        }
    }
}
