using System;
using IRabbitMQConnection = RabbitMQ.Client.IConnection;

namespace RabbitMQTopic
{
    /// <summary>
    /// 生产者配置
    /// </summary>
    public class ProducerSettings
    {
        /// <summary>
        /// 客户端名
        /// </summary>
        public string ClientName { get; set; }

        /// <summary>
        /// AMQP Uri（AmqpUri、AmqpConnection，至少设置一个）
        /// </summary>
        public Uri AmqpUri { get; set; }

        /// <summary>
        /// AMQP连接（AmqpUri、AmqpConnection，至少设置一个）
        /// </summary>
        public IRabbitMQConnection AmqpConnection { get; set; }

        /// <summary>
        /// 发送消息超时时间（默认：3s）
        /// </summary>
        public int SendMsgTimeout { get; set; }

        /// <summary>
        /// 最大Channel空闲时长（默认：10s）
        /// </summary>
        public long MaxChannelIdleDuration { get; set; }
    }
}
