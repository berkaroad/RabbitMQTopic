using RabbitMQ.Client;
using RabbitMQTopic.Internals;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IRabbitMQChannel = RabbitMQ.Client.IModel;
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
        private TimeSpan _sendMsgTimeout = TimeSpan.FromSeconds(3);
        private TimeSpan _maxIdleDuration = TimeSpan.FromSeconds(10);
        private ConcurrentQueue<RabbitMQChannelWithActiveTime> _channelPool;
        private int _maxChannelPoolSize = 1000;
        private volatile int _channelPoolSize;
        private bool _selfCreate = false;
        private bool _delayedMessageEnabled = false;
        private bool _autoConfig = false;
        private Dictionary<string, int> _topics = new Dictionary<string, int>();
        private volatile int _isRunning = 0;
        private Timer _cleanIdleChannelTimer;
        private int _cleanInterval = 1;

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
            if (settings.SendMsgTimeout > 0)
            {
                _sendMsgTimeout = TimeSpan.FromSeconds(settings.SendMsgTimeout);
            }
            if (settings.MaxChannelIdleDuration > 0)
            {
                _maxIdleDuration = TimeSpan.FromSeconds(settings.MaxChannelIdleDuration);
            }
            if (settings.MaxChannelPoolSize > 0)
            {
                _maxChannelPoolSize = settings.MaxChannelPoolSize;
            }
            _delayedMessageEnabled = delayedMessageEnabled;
            _autoConfig = autoConfig;
            _channelPool = new ConcurrentQueue<RabbitMQChannelWithActiveTime>();
            _cleanIdleChannelTimer = new Timer(ClearIdleChannel);
        }

        /// <summary>
        /// 注册Topic
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="queueCount">Topic的队列数（必须为2的幂）</param>
        /// <return></return>
        public Producer RegisterTopic(string topic, int queueCount)
        {
            if (_isRunning == 1)
            {
                throw new NotSupportedException("Couldn't register topic when is running.");
            }
            if (string.IsNullOrEmpty(topic))
            {
                throw new ArgumentNullException(nameof(topic), "must not empty.");
            }
            if (queueCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(queueCount), queueCount, "QueueCount must greater than zero.");
            }
            if ((queueCount & (queueCount - 1)) != 0)
            { 
                throw new ArgumentOutOfRangeException(nameof(queueCount), queueCount, "QueueCount must be the power of 2.");
            }
            if (!_topics.ContainsKey(topic))
            {
                _topics.Add(topic, queueCount);
            }
            return this;
        }

        /// <summary>
        /// 启动
        /// </summary>
        public void Start()
        {
            if (Interlocked.CompareExchange(ref _isRunning, 1, 0) == 0)
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

                if (_autoConfig)
                {
                    foreach (var topic in _topics.Keys)
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
                    }
                }
                _cleanIdleChannelTimer.Change(TimeSpan.FromSeconds(_cleanInterval), TimeSpan.FromSeconds(_cleanInterval));
            }
        }

        /// <summary>
        /// 关闭
        /// </summary>
        public void Shutdown()
        {
            if (Interlocked.CompareExchange(ref _isRunning, 0, 1) == 1)
            {
                _cleanIdleChannelTimer.Change(Timeout.Infinite, Timeout.Infinite);
                Thread.Sleep(100);
                while (_channelPool.TryDequeue(out RabbitMQChannelWithActiveTime item))
                {
                    if (item.Channel.IsOpen)
                    {
                        item.Channel.Close();
                    }
                }
                if (_amqpConnection != null)
                {
                    if (_selfCreate)
                    {
                        _amqpConnection.Close();
                    }
                    _amqpConnection = null;
                }
            }
        }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        /// <value></value>
        public bool IsRunning
        {
            get { return _isRunning == 1; }
        }

        /// <summary>
        /// 发送消息（异步）
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="routingKey">路由key</param>
        /// <returns></returns>
        public Task<SendResult> SendMessageAsync(Message message, string routingKey)
        {
            return Task.Factory.StartNew(() => SendMessage(message, routingKey), TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="routingKey">路由key</param>
        /// <returns></returns>
        public SendResult SendMessage(Message message, string routingKey)
        {
            if (_isRunning == 0)
            {
                return new SendResult(SendStatus.Failed, null, "Couldn't send message when is not running.");
            }
            if (message == null)
            {
                return new SendResult(SendStatus.Failed, null, "Message is null.");
            }
            if (!_topics.ContainsKey(message.Topic))
            {
                return new SendResult(SendStatus.Failed, null, $"Topic {message.Topic} not registered.");
            }
            RabbitMQChannelWithActiveTime item = null;
            try
            {
                var queueCount = _topics[message.Topic];
                var queueId = string.IsNullOrEmpty(routingKey) ? 0 : (Crc16.GetHashCode(routingKey) & (queueCount - 1));
                var messageId = Guid.NewGuid().ToString();
                var createdTime = message.CreatedTime == DateTime.MinValue ? DateTime.Now : message.CreatedTime;
                if (_channelPool.TryDequeue(out item))
                {
                    item.RefreshActiveTime();
                }
                else
                {
                    while (Interlocked.Increment(ref _channelPoolSize) > _maxChannelPoolSize)
                    {
                        Interlocked.Decrement(ref _channelPoolSize);
                        if (_channelPool.TryDequeue(out item))
                        {
                            item.RefreshActiveTime();
                            break;
                        }
                    }
                    if (item == null)
                    {
                        item = new RabbitMQChannelWithActiveTime(_amqpConnection.CreateModel());
                        item.Channel.ConfirmSelect();
                    }
                }
                var properties = item.Channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = message.ContentType ?? string.Empty;
                properties.MessageId = Guid.NewGuid().ToString();
                properties.Type = message.Tag ?? string.Empty;
                properties.Timestamp = new AmqpTimestamp(DateTime2UnixTime.ToUnixTime(createdTime));
                if (_delayedMessageEnabled && message.DelayedMilliseconds > 0)
                {
                    properties.Headers = new Dictionary<string, object>
                    {
                        { "x-delay", message.DelayedMilliseconds }
                    };
                }
                item.Channel.BasicPublish(exchange: _delayedMessageEnabled && message.DelayedMilliseconds > 0 ? $"{message.Topic}-delayed" : message.Topic,
                                     routingKey: queueId.ToString(),
                                     mandatory: true,
                                     basicProperties: properties,
                                     body: message.Body);
                if (!item.Channel.WaitForConfirms(_sendMsgTimeout, out bool timedOut))
                {
                    return new SendResult(timedOut ? SendStatus.Timeout : SendStatus.Failed, null, "Wait for confirms failed.");
                }
                var storeResult = new MessageStoreResult(messageId, message.Code, message.Topic, queueId, createdTime, message.Tag);
                return new SendResult(SendStatus.Success, storeResult, null);
            }
            catch (Exception ex)
            {
                throw new System.IO.IOException("Send message has exception.", ex);
            }
            finally
            {
                if (item != null)
                {
                    _channelPool.Enqueue(item);
                }
            }
        }

        private void ClearIdleChannel(object state)
        {
            _cleanIdleChannelTimer.Change(Timeout.Infinite, Timeout.Infinite);
            try
            {
                if (_channelPool.TryPeek(out RabbitMQChannelWithActiveTime item) && _isRunning == 1)
                {
                    if (item.ActiveTime.Add(_maxIdleDuration) < DateTime.Now)
                    {
                        if (_channelPool.TryDequeue(out RabbitMQChannelWithActiveTime removedItem))
                        {
                            Interlocked.Decrement(ref _channelPoolSize);
                            if (removedItem.Channel.IsOpen)
                            {
                                removedItem.Channel.Close();
                            }
                        }
                    }
                }
            }
            finally
            {
                if (_isRunning == 1)
                {
                    _cleanIdleChannelTimer.Change(TimeSpan.FromSeconds(_cleanInterval), TimeSpan.FromSeconds(_cleanInterval));
                }
            }
        }

        private class RabbitMQChannelWithActiveTime
        {
            public RabbitMQChannelWithActiveTime(IRabbitMQChannel channel)
            {
                Channel = channel;
                ActiveTime = DateTime.Now;
            }

            public IRabbitMQChannel Channel { get; private set; }

            public DateTime ActiveTime { get; private set; }

            public void RefreshActiveTime()
            {
                ActiveTime = DateTime.Now;
            }
        }
    }
}
