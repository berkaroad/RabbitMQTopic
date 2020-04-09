using RabbitMQ.Client;
using System;
using System.Threading.Tasks;
using IRabbitMQConnection = RabbitMQ.Client.IConnection;
using RabbitMQConnectionFactory = RabbitMQ.Client.ConnectionFactory;

namespace RabbitMQTopic
{
    /// <summary>
    /// 生产者
    /// </summary>
    public class Producer
    {
        private Uri _amqpUri = null;
        private string _clientName = null;
        private IRabbitMQConnection _amqpConnection = null;
        private bool _selfCreate = false;

        /// <summary>
        /// 生产者
        /// </summary>
        /// <param name="settings"></param>
        public Producer(ProducerSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            if (settings.AmqpConnection == null && settings.AmqpUri == null)
            {
                throw new ArgumentNullException("AmqpConnection or AmqpUri must be set.");
            }
            _clientName = string.IsNullOrEmpty(settings.ClientName) ? "undefined producer client" : settings.ClientName;
            _amqpUri = settings.AmqpUri;
            if (settings.AmqpConnection != null)
            {
                _amqpConnection = settings.AmqpConnection;
                _clientName = settings.AmqpConnection.ClientProvidedName;
            }
        }

        /// <summary>
        /// 启动
        /// </summary>
        public void Start()
        {
            if (_amqpConnection == null)
            {
                var connFactory = new RabbitMQConnectionFactory
                {
                    Uri = _amqpUri
                };
                _amqpConnection = connFactory.CreateConnection(_clientName);
                _selfCreate = true;
            }
        }

        /// <summary>
        /// 关闭
        /// </summary>
        public void Shutdown()
        {
            if (_amqpConnection != null && _selfCreate)
            {
                _amqpConnection.Close();
                _amqpConnection = null;
            }
        }

        /// <summary>
        /// 发送消息（异步）
        /// </summary>
        /// <param name="message"></param>
        /// <param name="routingKey"></param>
        /// <param name="messageId"></param>
        /// <returns></returns>
        public async Task SendMessageAsync(TopicMessage message, string routingKey, string messageId)
        {
            await Task.Run(() =>
            {
                SendMessage(message, routingKey, messageId);
            });
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="routingKey"></param>
        /// <param name="messageId"></param>
        public void SendMessage(TopicMessage message, string routingKey, string messageId)
        {
            try
            {
                var realRoutingKey = string.IsNullOrEmpty(routingKey) ? "0" : ((uint)routingKey.GetHashCode() % message.QueueCount).ToString();
                using (var channel = _amqpConnection.CreateModel())
                {
                    var properties = channel.CreateBasicProperties();
                    properties.Persistent = true;
                    properties.DeliveryMode = 2;
                    properties.ContentType = message.ContentType ?? string.Empty;
                    properties.MessageId = messageId;
                    properties.Type = message.Tag ?? string.Empty;
                    properties.Timestamp = new AmqpTimestamp(DateTime2UnixTime.ToUnixTime(message.CreatedTime));

                    channel.ConfirmSelect();
                    channel.BasicPublish(exchange: message.Topic,
                                         routingKey: realRoutingKey,
                                         mandatory: true,
                                         basicProperties: properties,
                                         body: message.Body);
                    channel.WaitForConfirmsOrDie();
                }
            }
            catch (Exception ex)
            {
                throw new System.IO.IOException("Send message has exception.", ex);
            }
        }
    }
}
