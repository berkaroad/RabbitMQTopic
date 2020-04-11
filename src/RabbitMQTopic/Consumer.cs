using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using IRabbitMQConnection = RabbitMQ.Client.IConnection;
using RabbitMQConnectionFactory = RabbitMQ.Client.ConnectionFactory;
using RabbitMQConstants = RabbitMQ.Client.Framing.Constants;

namespace RabbitMQTopic
{
    /// <summary>
    /// 消费者
    /// </summary>
    public class Consumer
    {
        private Uri _amqpUri = null;
        private string _clientName = null;
        private IRabbitMQConnection _amqpConnection = null;
        private bool _selfCreate = false;
        private ushort _prefetchCount = 0;
        private string _groupName = null;
        private int _consumerCount = 0;
        private int _consumerSequence = 0;
        private bool _autoConfig = false;
        private Dictionary<string, int> _topics = new Dictionary<string, int>();
        private ConcurrentDictionary<string, ConcurrentDictionary<byte, IModel>> _globalChannels = new ConcurrentDictionary<string, ConcurrentDictionary<byte, IModel>>();
        private ConcurrentDictionary<string, ConcurrentDictionary<byte, EventingBasicConsumer>> _globalConsumers = new ConcurrentDictionary<string, ConcurrentDictionary<byte, EventingBasicConsumer>>();

        /// <summary>
        /// 消息已接受事件
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> OnMessageReceived;

        /// <summary>
        /// 消费者
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="autoConfig">自动建Exchange、Queue和Bind</param>
        public Consumer(ConsumerSettings settings, bool autoConfig = true)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            if (settings.AmqpConnection == null && settings.AmqpUri == null)
            {
                throw new ArgumentNullException("AmqpConnection or AmqpUri must be set.");
            }
            _clientName = string.IsNullOrEmpty(settings.ClientName) ? "undefined consumer client" : settings.ClientName;
            _amqpUri = settings.AmqpUri;
            if (settings.AmqpConnection != null)
            {
                _amqpConnection = settings.AmqpConnection;
                _clientName = settings.AmqpConnection.ClientProvidedName;
            }
            _prefetchCount = settings.PrefetchCount <= 0 ? (ushort)1 : (ushort)settings.PrefetchCount;
            _groupName = string.IsNullOrEmpty(settings.GroupName) ? "default" : settings.GroupName;
            _consumerCount = settings.ConsumerCount <= 0 ? 1 : settings.ConsumerCount;
            _consumerSequence = settings.ConsumerSequence <= 0 || settings.ConsumerSequence > _consumerCount ? 1 : settings.ConsumerSequence;
            _autoConfig = autoConfig;
        }

        /// <summary>
        /// 订阅Topic
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="queueCount"></param>
        public void Subscribe(string topic, int queueCount)
        {
            if (string.IsNullOrEmpty(topic))
            {
                throw new ArgumentNullException(nameof(topic), "must not empty.");
            }
            if (queueCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(queueCount), queueCount, "QueueCount must greater than zero.");
            }
            if (!_topics.ContainsKey(topic))
            {
                _topics.Add(topic, queueCount);
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
            StartSubscribe();
        }

        /// <summary>
        /// 关闭
        /// </summary>
        public void Shutdown()
        {
            if (_amqpConnection != null)
            {
                foreach (var exchangeName in _globalChannels.Keys)
                {
                    var channels = _globalChannels[exchangeName];
                    var consumers = _globalConsumers[exchangeName];
                    foreach (var queueIndex in channels.Keys)
                    {
                        var channel = channels[queueIndex];
                        channel.Close(RabbitMQConstants.ConnectionForced, $"\"{exchangeName}-{queueIndex}\"'s normal channel disposed");
                    }
                    channels.Clear();
                    consumers.Clear();
                }
                _globalChannels.Clear();
                _globalConsumers.Clear();

                if (_selfCreate)
                {
                    _amqpConnection.Close();
                    _amqpConnection = null;
                }
            }
        }

        private void StartSubscribe()
        {
            foreach (var topic in _topics.Keys)
            {
                var queueCount = _topics[topic];
                var subscribeQueues = GetSubscribeQueues(queueCount, _consumerCount, _consumerSequence);
                var subTopic = $"{topic}.G.{_groupName}";
                var consumers = new ConcurrentDictionary<byte, EventingBasicConsumer>();
                var channels = new ConcurrentDictionary<byte, IModel>();
                if (_autoConfig)
                {
                    using (var channelForConfig = _amqpConnection.CreateModel())
                    {
                        channelForConfig.ExchangeDeclare(topic, ExchangeType.Fanout, true, false, null);
                        channelForConfig.ExchangeDeclare(subTopic, ExchangeType.Direct, true, false, null);
                        channelForConfig.ExchangeBind(subTopic, topic, "", null);

                        for (byte queueIndex = 0; queueIndex < queueCount; queueIndex++)
                        {
                            string queueName = $"{subTopic}-{queueIndex}";
                            channelForConfig.QueueDeclare(queueName, true, false, false, null);
                            channelForConfig.QueueBind(queueName, subTopic, queueIndex.ToString(), null);
                        }

                        channelForConfig.Close();
                    }
                }

                if (!_globalChannels.TryAdd(subTopic, channels)
                    || !_globalConsumers.TryAdd(subTopic, consumers))
                {
                    throw new Exception($"{subTopic} has subscribed.");
                }

                for (byte queueIndex = 0; queueIndex < queueCount; queueIndex++)
                {
                    if (subscribeQueues == null || !subscribeQueues.Any(a => a == queueIndex))
                    {
                        continue;
                    }
                    consumers.TryGetValue(queueIndex, out EventingBasicConsumer consumer);
                    if (consumer != null)
                    {
                        continue;
                    }
                    channels.TryGetValue(queueIndex, out IModel channel);
                    if (channel == null)
                    {
                        channel = _amqpConnection.CreateModel();
                        channel.BasicQos(0, _prefetchCount, false);
                    }
                    consumer = new EventingBasicConsumer(channel);

                    try
                    {
                        consumer.Received += (sender, e) =>
                        {
                            var currentConsumer = ((EventingBasicConsumer)sender);
                            var currentChannel = currentConsumer.Model;
                            while (!channels.Any(w => w.Value == currentChannel))
                            {
                                Thread.Sleep(10);
                            }
                            var currentQueueIndex = channels.First(w => w.Value == currentChannel).Key;
                            var currentQueueName = $"{subTopic}-{currentQueueIndex}";

                            if (OnMessageReceived != null)
                            {
                                var context = new MessageHandlingTransportationContext(e.Exchange, currentQueueName, channel, e.DeliveryTag, new Dictionary<string, object> {
                                    { MessagePropertyConstants.MESSAGE_ID, e.BasicProperties.MessageId },
                                    { MessagePropertyConstants.MESSAGE_TYPE, e.BasicProperties.Type },
                                    { MessagePropertyConstants.TIMESTAMP, e.BasicProperties.Timestamp.UnixTime == 0 ? DateTime.Now : DateTime2UnixTime.FromUnixTime(e.BasicProperties.Timestamp.UnixTime) },
                                    { MessagePropertyConstants.CONTENT_TYPE, string.IsNullOrEmpty(e.BasicProperties.ContentType) ? "text/json" : e.BasicProperties.ContentType },
                                    { MessagePropertyConstants.BODY, e.Body },
                                    { MessagePropertyConstants.ROUTING_KEY, e.RoutingKey }
                                });
                                OnMessageReceived.Invoke(this, new MessageReceivedEventArgs(context));
                            }
                        };

                        consumer.Shutdown += (sender, e) =>
                        {
                            var currentConsumer = ((EventingBasicConsumer)sender);
                            var currentChannel = currentConsumer.Model;
                            if (e.ReplyCode == RabbitMQConstants.ConnectionForced)
                            {
                                return;
                            }
                            while (e.ReplyCode == RabbitMQConstants.ChannelError && !currentChannel.IsOpen)
                            {
                                Thread.Sleep(10);
                            }
                            while (!channels.Any(w => w.Value == currentChannel))
                            {
                                Thread.Sleep(10);
                            }
                            var currentQueueIndex = channels.First(w => w.Value == currentChannel).Key;
                            var currentQueueName = $"{subTopic}-{currentQueueIndex}";
                        };

                        channel.BasicConsume($"{subTopic}-{queueIndex}", false, $"{_amqpConnection.ClientProvidedName}_consumer{queueIndex}", new Dictionary<string, object>(), consumer);
                    }
                    finally
                    {
                        channels.TryAdd(queueIndex, channel);
                        consumers.TryAdd(queueIndex, consumer);
                    }
                }
            }
        }

        private IEnumerable<byte> GetSubscribeQueues(int queueCount, int consumerCount, int consumerSequence)
        {
            if (consumerSequence > queueCount)
            {
                yield break;
            }
            else
            {
                for (byte queueIndex = 0; queueIndex < queueCount; queueIndex++)
                {
                    if (queueIndex % consumerCount == consumerSequence - 1)
                    {
                        yield return queueIndex;
                    }
                }
            }
        }
    }
}
