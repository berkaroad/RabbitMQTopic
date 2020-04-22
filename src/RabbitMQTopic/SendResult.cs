namespace RabbitMQTopic
{
    /// <summary>
    /// 发送结果
    /// </summary>
    public class SendResult
    {
        /// <summary>
        /// 发送结果
        /// </summary>
        /// <param name="sendStatus"></param>
        /// <param name="messageStoreResult"></param>
        /// <param name="errorMessage"></param>
        public SendResult(SendStatus sendStatus, MessageStoreResult messageStoreResult, string errorMessage)
        {
            SendStatus = sendStatus;
            MessageStoreResult = messageStoreResult;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// 发送状态
        /// </summary>
        public SendStatus SendStatus { get; private set; }

        /// <summary>
        /// 消息存储结果
        /// </summary>
        public MessageStoreResult MessageStoreResult { get; private set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"[SendStatus={SendStatus},MessageStoreResults={MessageStoreResult},ErrorMessage={ErrorMessage}]";
        }
    }
}