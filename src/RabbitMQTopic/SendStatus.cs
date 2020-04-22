namespace RabbitMQTopic
{
    /// <summary>
    /// 发送状态
    /// </summary>
    public enum SendStatus
    {
        /// <summary>
        /// 成功
        /// </summary>
        Success = 0,
        /// <summary>
        /// 超时
        /// </summary>
        Timeout = 1,
        /// <summary>
        /// 失败
        /// </summary>
        Failed = 2
    }
}