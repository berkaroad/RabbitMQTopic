using System.Collections.Generic;

namespace RabbitMQTopic
{
    /// <summary>
    /// 消息传输上下文
    /// </summary>
    public interface IMessageTransportationContext
    {
        /// <summary>
        /// Topic
        /// </summary>
        string Topic { get; }

        /// <summary>
        /// 队列索引
        /// </summary>
        int QueueIndex { get; }

        /// <summary>
        /// 消费组名
        /// </summary>
        string GroupName{get;}

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
