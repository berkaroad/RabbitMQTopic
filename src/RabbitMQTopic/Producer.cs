using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private bool _delayedMessageEnabled = false;
        private bool _autoConfig = false;
        private ConcurrentDictionary<string, bool> _configuredTopics = new ConcurrentDictionary<string, bool>();
        private object _configLocker = new object();
        
        /// <summary>
        /// 生产者
        /// </summary>
        /// <param name="settings"></param>
        public Producer(ProducerSettings settings)
            : this(settings, false, true) { }

        /// <summary>
        /// 生产者
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="delayedMessageEnabled">延迟消息已启用（需启用插件 rabbitmq_delayed_message_exchange）</param>
        public Producer(ProducerSettings settings, bool delayedMessageEnabled)
            : this(settings, delayedMessageEnabled, true) { }

        /// <summary>
        /// 生产者
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="delayedMessageEnabled">延迟消息已启用（需启用插件 rabbitmq_delayed_message_exchange）</param>
        /// <param name="autoConfig">自动建Exchange和Bind</param>
        public Producer(ProducerSettings settings, bool delayedMessageEnabled, bool autoConfig)
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
            _delayedMessageEnabled = delayedMessageEnabled;
            _autoConfig = autoConfig;
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
            if (_amqpConnection != null)
            {
                if (_selfCreate)
                {
                    _amqpConnection.Close();
                }
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
        public Task SendMessageAsync(TopicMessage message, string routingKey, string messageId)
        {
            new Task(() => SendMessage(message, routingKey, messageId), TaskCreationOptions.LongRunning).Start();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="routingKey"></param>
        /// <param name="messageId"></param>
        public void SendMessage(TopicMessage message, string routingKey, string messageId)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            try
            {
                Config(message.Topic);
                var realRoutingKey = string.IsNullOrEmpty(routingKey) ? "0" : ((uint)routingKey.GetHashCode() % message.QueueCount).ToString();
                using (var channel = _amqpConnection.CreateModel())
                {
                    channel.ConfirmSelect();
                    var properties = channel.CreateBasicProperties();
                    properties.Persistent = true;
                    properties.DeliveryMode = 2;
                    properties.ContentType = message.ContentType ?? string.Empty;
                    properties.MessageId = messageId;
                    properties.Type = message.Tag ?? string.Empty;
                    properties.Timestamp = new AmqpTimestamp(DateTime2UnixTime.ToUnixTime(message.CreatedTime));
                    if (_delayedMessageEnabled && message.DelayedMillisecond > 0)
                    {
                        properties.Headers = new Dictionary<string, object>
                        {
                            { "x-delay", message.DelayedMillisecond }
                        };
                    }
                    channel.BasicPublish(exchange: _delayedMessageEnabled && message.DelayedMillisecond > 0 ? $"{message.Topic}-delayed" : message.Topic,
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

        private void Config(string topic)
        {
            if (_autoConfig)
            {
                if (!_configuredTopics.ContainsKey(topic))
                {
                    lock (_configLocker)
                    {
                        if (!_configuredTopics.ContainsKey(topic))
                        {
                            using (var channelForConfig = _amqpConnection.CreateModel())
                            {
                                channelForConfig.ExchangeDeclare(topic, ExchangeType.Fanout, true, false, null);
                                if (_delayedMessageEnabled)
                                {
                                    channelForConfig.ExchangeDeclare($"{topic}-delayed", "x-delayed-message", true, false, new Dictionary<string, object>
                                    {
                                        { "x-delayed-type", ExchangeType.Fanout }
                                    });
                                    channelForConfig.ExchangeBind(topic, $"{topic}-delayed", "", null);
                                }
                                channelForConfig.Close();
                            }
                            _configuredTopics.TryAdd(topic, true);
                        }
                    }
                }
            }
        }
    }
}
