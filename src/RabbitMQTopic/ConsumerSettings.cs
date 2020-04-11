using System;
using IRabbitMQConnection = RabbitMQ.Client.IConnection;

namespace RabbitMQTopic
{
    /// <summary>
    /// 消费者设置
    /// </summary>
    public class ConsumerSettings
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
        /// 消费者组名
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// 消费者个数（用于负载自动分配队列）
        /// </summary>
        public int ConsumerCount { get; set; }

        /// <summary>
        /// 消费者序号，从1开始（用于负载自动分配队列）
        /// </summary>
        public int ConsumerSequence { get; set; }

        /// <summary>
        /// 每个队列的预抓取消息数
        /// </summary>
        public int PrefetchCount { get; set; }
    }
}
