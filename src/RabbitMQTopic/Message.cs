using System;

namespace RabbitMQTopic
{
    /// <summary>
    /// 消息
    /// </summary>
    [Serializable]
    public class Message
    {
        /// <summary>
        /// Topic（对应Exchange）
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// 代码（消息类别）
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// 创建时间（时间戳）
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// Tag标签（对应消息体的数据类型）
        /// </summary>
        public string Tag { get; set; }

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
        public int DelayedMilliseconds { get; set; }

        /// <summary>
        /// Topic消息
        /// </summary>
        public Message()
        {
        }

        /// <summary>
        /// 消息
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="code"></param>
        /// <param name="body"></param>
        /// <param name="contentType"></param>
        /// <param name="tag"></param>
        public Message(string topic, int code, byte[] body, string contentType, string tag = null)
            : this(topic, code, body, contentType, DateTime.Now, 0, tag)
        {
        }

        /// <summary>
        /// 消息
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="code"></param>
        /// <param name="body"></param>
        /// <param name="contentType"></param>
        /// <param name="createdTime"></param>
        /// <param name="tag"></param>
        public Message(string topic, int code, byte[] body, string contentType, DateTime createdTime, string tag = null)
            : this(topic, code, body, contentType, createdTime, 0, tag)
        {
        }

        /// <summary>
        /// 消息
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="code"></param>
        /// <param name="body"></param>
        /// <param name="contentType"></param>
        /// <param name="delayedMilliseconds"></param>
        /// <param name="tag"></param>
        public Message(string topic, int code, byte[] body, string contentType, int delayedMilliseconds,
            string tag = null)
            : this(topic, code, body, contentType, DateTime.Now, delayedMilliseconds, tag)
        {
        }

        /// <summary>
        /// 消息
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="code"></param>
        /// <param name="body"></param>
        /// <param name="contentType"></param>
        /// <param name="createdTime"></param>
        /// <param name="delayedMilliseconds"></param>
        /// <param name="tag"></param>
        public Message(string topic, int code, byte[] body, string contentType, DateTime createdTime,
            int delayedMilliseconds, string tag = null)
        {
            if (string.IsNullOrEmpty(topic))
            {
                throw new ArgumentNullException(nameof(topic));
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
            Code = code;
            Tag = tag;
            CreatedTime = createdTime;
            DelayedMilliseconds = delayedMilliseconds;
            Body = body;
            ContentType = contentType;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return
                $"[Topic={Topic},Code={Code},Tag={Tag},CreatedTime={CreatedTime},DelayedMilliseconds={DelayedMilliseconds},BodyLength={Body.Length},ContentType={ContentType}]";
        }
    }
}
