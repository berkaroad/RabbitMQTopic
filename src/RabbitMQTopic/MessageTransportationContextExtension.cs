using RabbitMQTopic.Internals;
using System;

namespace RabbitMQTopic
{
    /// <summary>
    /// 消息传输上下文扩展
    /// </summary>
    public static class MessageTransportationContextExtension
    {
        /// <summary>
        /// 获取消息ID
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string GetMessageId(this IMessageTransportationContext context)
        {
            return context.Properties.ContainsKey(MessagePropertyConstants.MESSAGE_ID) ? (string)context.Properties[MessagePropertyConstants.MESSAGE_ID] : "";
        }

        /// <summary>
        /// 获取消息类型
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string GetMessageType(this IMessageTransportationContext context)
        {
            return context.Properties.ContainsKey(MessagePropertyConstants.MESSAGE_TYPE) ? (string)context.Properties[MessagePropertyConstants.MESSAGE_TYPE] : "";
        }

        /// <summary>
        /// 获取消息时间戳（如果未设置，则返回当前时间）
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static DateTime GetTimestamp(this IMessageTransportationContext context)
        {
            return context.Properties.ContainsKey(MessagePropertyConstants.TIMESTAMP) ? (DateTime)context.Properties[MessagePropertyConstants.TIMESTAMP] : DateTime.Now;
        }

        /// <summary>
        /// 获取MIME内容类型
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string GetContentType(this IMessageTransportationContext context)
        {
            return context.Properties.ContainsKey(MessagePropertyConstants.CONTENT_TYPE) ? (string)context.Properties[MessagePropertyConstants.CONTENT_TYPE] : "";
        }

        /// <summary>
        /// 获取消息重放数据
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static byte[] GetBody(this IMessageTransportationContext context)
        {
            return context.Properties.ContainsKey(MessagePropertyConstants.BODY) ? (byte[])context.Properties[MessagePropertyConstants.BODY] : null;
        }

        /// <summary>
        /// 获取路由Key
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string GetRoutingKey(this IMessageTransportationContext context)
        {
            return context.Properties.ContainsKey(MessagePropertyConstants.ROUTING_KEY) ? (string)context.Properties[MessagePropertyConstants.ROUTING_KEY] : "";
        }
    }
}
