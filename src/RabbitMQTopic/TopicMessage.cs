using System;

namespace RabbitMQTopic
{
    /// <summary>
    /// Topic消息
    /// </summary>
    [Serializable]
    public class TopicMessage
    {
        /// <summary>
        /// Topic（对应Exchange）
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// 队列个数
        /// </summary>
        public int QueueCount { get; set; }

        /// <summary>
        /// Tag标签（对应消息体的数据类型）
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// 代码（消息类别）
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// 消息体
        /// </summary>
        public byte[] Body { get; set; }

        /// <summary>
        /// 内容类型（MIME）
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// 延迟毫秒数
        /// </summary>
        public int DelayedMillisecond { get; set; }

        /// <summary>
        /// 创建时间（时间戳）
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// Topic消息
        /// </summary>
        public TopicMessage() { }

        /// <summary>
        /// Topic消息
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="queueCount"></param>
        /// <param name="code"></param>
        /// <param name="body"></param>
        /// <param name="contentType"></param>
        /// <param name="tag"></param>
        public TopicMessage(string topic, int queueCount, int code, byte[] body, string contentType, string tag = null)
            : this(topic, queueCount, code, body, contentType, DateTime.Now, 0, tag) { }

        /// <summary>
        /// Topic消息
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="queueCount"></param>
        /// <param name="code"></param>
        /// <param name="body"></param>
        /// <param name="contentType"></param>
        /// <param name="createdTime"></param>
        /// <param name="tag"></param>
        public TopicMessage(string topic, int queueCount, int code, byte[] body, string contentType, DateTime createdTime, string tag = null)
            : this(topic, queueCount, code, body, contentType, createdTime, 0, tag) { }

        /// <summary>
        /// Topic消息
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="queueCount"></param>
        /// <param name="code"></param>
        /// <param name="body"></param>
        /// <param name="contentType"></param>
        /// <param name="delayedMillisecond"></param>
        /// <param name="tag"></param>
        public TopicMessage(string topic, int queueCount, int code, byte[] body, string contentType, int delayedMillisecond = 0, string tag = null)
            : this(topic, queueCount, code, body, contentType, DateTime.Now, delayedMillisecond, tag) { }

        /// <summary>
        /// Topic消息
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="queueCount"></param>
        /// <param name="code"></param>
        /// <param name="body"></param>
        /// <param name="contentType"></param>
        /// <param name="createdTime"></param>
        /// <param name="delayedMillisecond"></param>
        /// <param name="tag"></param>
        public TopicMessage(string topic, int queueCount, int code, byte[] body, string contentType, DateTime createdTime, int delayedMillisecond, string tag = null)
        {
            if (string.IsNullOrEmpty(topic))
            {
                throw new ArgumentNullException(nameof(topic));
            }
            if (queueCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(queueCount), queueCount, "QueueCount must greater than zero.");
            }
            if (code <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(code), code, "Code must greater than zero.");
            }
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }
            Topic = topic;
            QueueCount = queueCount;
            Code = code;
            Tag = tag;
            CreatedTime = createdTime;
            DelayedMillisecond = delayedMillisecond;
            Body = body;
            ContentType = contentType;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"[Topic={Topic},QueueCount={QueueCount},Code={Code},Tag={Tag},CreatedTime={CreatedTime},DelayedMillisecond={DelayedMillisecond},BodyLength={Body.Length},ContentType={ContentType}]";
        }
    }
}
