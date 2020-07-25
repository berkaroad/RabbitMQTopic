using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace RabbitMQTopic.Internals
{
    internal class MessageHandlingTransportationContext : IMessageTransportationContext
    {
        private readonly IModel _channel;
        private readonly ulong _deliveryTag;

        public MessageHandlingTransportationContext(string topic, int queueIndex, string groupName, IModel channel,
            ulong deliveryTag, IDictionary<string, object> properties)
        {
            Topic = topic;
            QueueIndex = queueIndex;
            GroupName = groupName;
            _channel = channel;
            _deliveryTag = deliveryTag;
            Properties = properties;
        }

        public string Topic { get; private set; }

        public int QueueIndex { get; private set; }

        public string GroupName { get; private set; }

        public IDictionary<string, object> Properties { get; private set; }

        public event EventHandler OnAck;

        public void Ack()
        {
            _channel.BasicAck(_deliveryTag, false);
            OnAck?.Invoke(this, new EventArgs());
        }
    }
}
