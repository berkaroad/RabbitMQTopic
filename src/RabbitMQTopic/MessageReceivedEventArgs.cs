using System;

namespace RabbitMQTopic
{
    /// <summary>
    /// 消息已接受事件参数
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// 消息已接受事件参数
        /// </summary>
        /// <param name="context"></param>
        public MessageReceivedEventArgs(IMessageTransportationContext context)
        {
            Context = context;
        }

        /// <summary>
        /// 消息传输上下文
        /// </summary>
        /// <value></value>
        public IMessageTransportationContext Context { get; private set; }
    }
}
