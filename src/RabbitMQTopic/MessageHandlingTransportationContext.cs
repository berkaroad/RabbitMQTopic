using RabbitMQ.Client;
using System.Collections.Generic;

namespace RabbitMQTopic
{
    internal class MessageHandlingTransportationContext : IMessageTransportationContext
    {
        private IModel _channel;

        public MessageHandlingTransportationContext(string exchangeName, string queueName, IModel channel, ulong deliveryTag, IDictionary<string, object> properties)
        {
            ExchangeName = exchangeName;
            QueueName = queueName;
            _channel = channel;
            DeliveryTag = deliveryTag;
            Properties = properties;
        }

        public string ExchangeName { get; private set; }

        public string QueueName { get; private set; }

        public ulong DeliveryTag { get; private set; }

        public IDictionary<string, object> Properties { get; private set; }

        public void Ack()
        {
            _channel.BasicAck(DeliveryTag, false);
        }
    }
}
