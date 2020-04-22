using System;

namespace RabbitMQTopic
{
    /// <summary>
    /// 消息存储结果
    /// </summary>
    [Serializable]
    public class MessageStoreResult
    {
        /// <summary>
        /// 消息存储结果
        /// </summary>
        public MessageStoreResult() { }

        /// <summary>
        /// 消息存储结果
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="code"></param>
        /// <param name="topic"></param>
        /// <param name="queueId"></param>
        /// <param name="createdTime"></param>
        /// <param name="tag"></param>
        public MessageStoreResult(string messageId, int code, string topic, int queueId, DateTime createdTime, string tag = null)
        {
            MessageId = messageId;
            Code = code;
            Topic = topic;
            QueueId = queueId;
            CreatedTime = createdTime;
            Tag = tag;
        }

        /// <summary>
        /// 消息ID
        /// </summary>
        /// <value></value>
        public string MessageId { get; private set; }

        /// <summary>
        /// Code
        /// </summary>
        /// <value></value>
        public int Code { get; private set; }

        /// <summary>
        /// Topic
        /// </summary>
        /// <value></value>
        public string Topic { get; private set; }

        /// <summary>
        /// Tag
        /// </summary>
        /// <value></value>
        public string Tag { get; private set; }

        /// <summary>
        /// 队列ID
        /// </summary>
        /// <value></value>
        public int QueueId { get; private set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        /// <value></value>
        public DateTime CreatedTime { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"[MessageId={MessageId},Code={Code},Topic={Topic},Tag={Tag},QueueId={QueueId},CreatedTime={CreatedTime}]";
        }
    }
}