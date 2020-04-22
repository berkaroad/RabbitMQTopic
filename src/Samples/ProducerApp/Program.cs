using System;
using RabbitMQTopic;

namespace ProducerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var producer = new Producer(new ProducerSettings
            {
                AmqpUri = new Uri("amqp://demo:123456@localhost/test"),
                ClientName = "ProducerApp",
                SendMsgTimeout = 1000,
            }, true);

            producer.RegisterTopic("CommandTopic", 4).Start();
            Console.WriteLine("Producer started!");
            var random = new Random();
            for (var i = 1; i <= 20; i++)
            {
                var messageId = Guid.NewGuid().ToString();
                var routingKey = Guid.NewGuid().ToString();
                var body = System.Text.Encoding.UTF8.GetBytes($"{i} delayed message {messageId}");
                var topicMessage = new Message("CommandTopic", 1, body, "text/json", 1000 * random.Next(1, 5), tag: "System.String");
                producer.SendMessage(topicMessage, routingKey);
            }
            producer.Shutdown();
            Console.WriteLine("Producer shutdown!");
        }
    }
}
