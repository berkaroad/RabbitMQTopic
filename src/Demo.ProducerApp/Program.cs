using System;
using RabbitMQTopic;

namespace Demo.ProducerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var producer = new Producer(new ProducerSettings
            {
                AmqpUri = new Uri("amqp://demo:123456@localhost/test"),
                ClientName = "ProducerApp"
            }, delayedMessageEnabled: true);

            producer.Start();
            for (var i = 1; i <= 10; i++)
            {
                var body = System.Text.Encoding.UTF8.GetBytes($"delayed message {i}");
                var topicMessage = new TopicMessage("CommandTopic", 4, 1, body, "text/json", "System.String", delayedMillisecond: 1000 * (15 - i));
                producer.SendMessage(topicMessage, "routingKey", "messageId");
            }
            Console.WriteLine("Producer started!");
            Console.ReadLine();
            producer.Shutdown();
        }
    }
}
