using System.Collections.Generic;

namespace RabbitMQTopic
{
    /// <summary>
    /// 消息传输上下文
    /// </summary>
    public interface IMessageTransportationContext
    {
        /// <summary>
        /// 交换器名
        /// </summary>
        string ExchangeName { get; }

        /// <summary>
        /// 队列名
        /// </summary>
        string QueueName { get; }

        /// <summary>
        /// DeliveryTag
        /// </summary>
        ulong DeliveryTag { get; }

        /// <summary>
        /// 属性集合
        /// </summary>
        IDictionary<string, object> Properties { get; }

        /// <summary>
        /// 消息应答
        /// </summary>
        void Ack();
    }
}
