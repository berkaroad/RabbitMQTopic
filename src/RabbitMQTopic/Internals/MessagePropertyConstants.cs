namespace RabbitMQTopic.Internals
{
    /// <summary>
    /// 消息属性常量
    /// </summary>
    internal class MessagePropertyConstants
    {
        /// <summary>
        /// 消息ID（string）
        /// </summary>
        public const string MESSAGE_ID = "MessageId";

        /// <summary>
        /// 消息类型（string）
        /// </summary>
        public const string MESSAGE_TYPE = "MessageType";

        /// <summary>
        /// 消息时间戳（DateTime）
        /// </summary>
        public const string TIMESTAMP = "Timestamp";

        /// <summary>
        /// 消息MIME内容类型（string）
        /// </summary>
        public const string CONTENT_TYPE = "ContentType";

        /// <summary>
        /// 消息体（byte[]）
        /// </summary>
        public const string BODY = "Body";

        /// <summary>
        /// 路由Key（byte）
        /// </summary>
        public const string ROUTING_KEY = "RoutingKey";
    }
}
