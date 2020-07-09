using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQTopic.Internals;
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
        private ConsumeMode _mode;
        private ushort _prefetchCount = 0;
        private string _groupName = null;
        private int _consumerCount = 0;
        private int _consumerSequence = 0;
        private bool _autoConfig = false;
        private Dictionary<string, int> _topics = new Dictionary<string, int>();
        private ConcurrentDictionary<string, ConcurrentDictionary<byte, Tuple<IModel, Thread>>> _globalChannels = new ConcurrentDictionary<string, ConcurrentDictionary<byte, Tuple<IModel, Thread>>>();
        private ConcurrentDictionary<string, ConcurrentDictionary<byte, EventingBasicConsumer>> _globalConsumers = new ConcurrentDictionary<string, ConcurrentDictionary<byte, EventingBasicConsumer>>();
        private volatile int _isRunning = 0;

        /// <summary>
        /// 消息已接受事件
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> OnMessageReceived;

        /// <summary>
        /// 消费者
        /// </summary>
        /// <param name="settings"></param>
        public Consumer(ConsumerSettings settings)
            : this(settings, true) { }

        /// <summary>
        /// 消费者
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="autoConfig">自动建Exchange、Queue和Bind</param>
        public Consumer(ConsumerSettings settings, bool autoConfig)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            if (settings.AmqpConnection == null && settings.AmqpUri == null)
            {
                throw new ArgumentNullException("AmqpConnection or AmqpUri must be set.");
            }
            if (settings.GroupName == "default")
            {
                throw new ArgumentException("GroupName cann't use reserve keywords \"default\".");
            }
            _clientName = string.IsNullOrEmpty(settings.ClientName) ? "undefined consumer client" : settings.ClientName;
            _amqpUri = settings.AmqpUri;
            if (settings.AmqpConnection != null)
            {
                _amqpConnection = settings.AmqpConnection;
                _clientName = settings.AmqpConnection.ClientProvidedName;
            }
            _mode = settings.Mode;
            _prefetchCount = settings.PrefetchCount <= 0 ? (ushort)1 : (ushort)settings.PrefetchCount;
            _groupName = settings.GroupName == null ? string.Empty : settings.GroupName;
            _consumerCount = settings.ConsumerCount <= 0 ? 1 : settings.ConsumerCount;
            _consumerSequence = settings.ConsumerSequence <= 0 || settings.ConsumerSequence > _consumerCount ? 1 : settings.ConsumerSequence;
            _autoConfig = autoConfig;
        }

        /// <summary>
        /// 订阅Topic
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="queueCount">Topic的队列数（必须为2的幂）</param>
        /// <return></return>
        public Consumer Subscribe(string topic, int queueCount)
        {
            if (_isRunning == 1)
            {
                throw new NotSupportedException("Couldn't subscribe topic when is running.");
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
                    using (var channelForConfig = _amqpConnection.CreateModel())
                    {
                        foreach (var topic in _topics.Keys)
                        {
                            var queueCount = _topics[topic];
                            var subTopic = GetSubTopic(topic);
                            channelForConfig.ExchangeDeclare(topic, ExchangeType.Fanout, true, false, null);
                            channelForConfig.ExchangeDeclare(subTopic, ExchangeType.Direct, true, false, null);
                            channelForConfig.ExchangeBind(subTopic, topic, "", null);

                            for (byte queueIndex = 0; queueIndex < queueCount; queueIndex++)
                            {
                                string queueName = GetQueue(topic, queueIndex);
                                channelForConfig.QueueDeclare(queueName, true, false, false, null);
                                channelForConfig.QueueBind(queueName, subTopic, queueIndex.ToString(), null);
                            }
                        }
                        channelForConfig.Close();
                    }
                }

                if (_mode == ConsumeMode.Push)
                {
                    ConsumeByPush();
                }
                else
                {
                    ConsumeByPull();
                }
            }
        }

        /// <summary>
        /// 关闭
        /// </summary>
        public void Shutdown()
        {
            if (Interlocked.CompareExchange(ref _isRunning, 0, 1) == 1)
            {
                Thread.Sleep(100);
                if (_amqpConnection != null)
                {
                    if (_mode == ConsumeMode.Push)
                    {
                        foreach (var topic in _globalConsumers.Keys)
                        {
                            var consumers = _globalConsumers[topic];
                            foreach (var queueIndex in consumers.Keys)
                            {
                                var channel = consumers[queueIndex].Model;
                                if (channel.IsOpen)
                                {
                                    channel.Close(RabbitMQConstants.ConnectionForced, $"\"{topic}-{queueIndex}\"'s normal channel disposed");
                                }
                            }
                            consumers.Clear();
                        }
                        _globalConsumers.Clear();
                    }
                    else
                    {
                        foreach (var topic in _globalChannels.Keys)
                        {
                            var channels = _globalChannels[topic];
                            foreach (var queueIndex in channels.Keys)
                            {
                                var channel = channels[queueIndex].Item1;
                                var consumerThread = channels[queueIndex].Item2;
                                if (channel.IsOpen)
                                {
                                    channel.Close(RabbitMQConstants.ConnectionForced, $"\"{topic}-{queueIndex}\"'s normal channel disposed");
                                }
                            }
                            channels.Clear();
                        }
                        _globalChannels.Clear();
                    }

                    if (_selfCreate)
                    {
                        _amqpConnection.Close();
                        _amqpConnection = null;
                    }
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

        private void ConsumeByPush()
        {
            foreach (var topic in _topics.Keys)
            {
                var queueCount = _topics[topic];
                var subscribeQueues = GetSubscribeQueues(queueCount, _consumerCount, _consumerSequence);
                var subTopic = GetSubTopic(topic);
                var consumers = new ConcurrentDictionary<byte, EventingBasicConsumer>();

                if (!_globalConsumers.TryAdd(subTopic, consumers))
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
                    var channel = _amqpConnection.CreateModel();
                    channel.BasicQos(0, _prefetchCount, false);
                    consumer = new EventingBasicConsumer(channel);

                    try
                    {
                        consumer.Received += (sender, e) =>
                        {
                            var currentConsumer = ((EventingBasicConsumer)sender);
                            var currentChannel = currentConsumer.Model;
                            while (!consumers.Any(w => w.Value.Model == currentChannel))
                            {
                                Thread.Sleep(1000);
                            }

                            if (OnMessageReceived != null)
                            {
                                var currentTopic = e.Exchange.IndexOf("-delayed") > 0 ? e.Exchange.Substring(0, e.Exchange.LastIndexOf("-delayed")) : e.Exchange;
                                var currentQueueIndex = consumers.First(w => w.Value.Model == currentChannel).Key;
                                var context = new MessageHandlingTransportationContext(currentTopic, currentQueueIndex, _groupName, channel, e.DeliveryTag, new Dictionary<string, object> {
                                    { MessagePropertyConstants.MESSAGE_ID, e.BasicProperties.MessageId },
                                    { MessagePropertyConstants.MESSAGE_TYPE, e.BasicProperties.Type },
                                    { MessagePropertyConstants.TIMESTAMP, e.BasicProperties.Timestamp.UnixTime == 0 ? DateTime.Now : DateTime2UnixTime.FromUnixTime(e.BasicProperties.Timestamp.UnixTime) },
                                    { MessagePropertyConstants.CONTENT_TYPE, string.IsNullOrEmpty(e.BasicProperties.ContentType) ? "text/json" : e.BasicProperties.ContentType },
                                    { MessagePropertyConstants.BODY, e.Body },
                                    { MessagePropertyConstants.ROUTING_KEY, e.RoutingKey }
                                });
                                try
                                {
                                    OnMessageReceived.Invoke(this, new MessageReceivedEventArgs(context));
                                }
                                catch { }
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
                                Thread.Sleep(1000);
                            }
                        };
                        channel.BasicConsume(GetQueue(topic, queueIndex), false, $"{_amqpConnection.ClientProvidedName}_consumer{queueIndex}", new Dictionary<string, object>(), consumer);
                    }
                    finally
                    {
                        consumers.TryAdd(queueIndex, consumer);
                    }
                }
            }
        }

        private void ConsumeByPull()
        {
            foreach (var topic in _topics.Keys)
            {
                var queueCount = _topics[topic];
                var subscribeQueues = GetSubscribeQueues(queueCount, _consumerCount, _consumerSequence);
                var subTopic = GetSubTopic(topic);
                var channels = new ConcurrentDictionary<byte, Tuple<IModel, Thread>>();
                if (!_globalChannels.TryAdd(subTopic, channels))
                {
                    throw new Exception($"{subTopic} has subscribed.");
                }

                for (byte queueIndex = 0; queueIndex < queueCount; queueIndex++)
                {
                    if (subscribeQueues == null || !subscribeQueues.Any(a => a == queueIndex))
                    {
                        continue;
                    }
                    channels.TryGetValue(queueIndex, out Tuple<IModel, Thread> channelTuple);
                    if (channelTuple != null)
                    {
                        continue;
                    }

                    try
                    {
                        var queueName = GetQueue(topic, queueIndex);
                        var channel = _amqpConnection.CreateModel();
                        var consumerThread = new Thread((state) =>
                        {
                            var currentChannelTopicQueueIndexPair = (Tuple<IModel, string, int>)state;
                            var currentChannel = currentChannelTopicQueueIndexPair.Item1;
                            var currentTopic = currentChannelTopicQueueIndexPair.Item2;
                            var currentQueueIndex = currentChannelTopicQueueIndexPair.Item3;
                            var currentQueueName = GetQueue(currentTopic, currentQueueIndex);
                            int unackCount = 0;
                            int noMsgCount = 0;
                            while (true)
                            {
                                if (!currentChannel.IsOpen)
                                {
                                    var closeReason = currentChannel.CloseReason;
                                    if (closeReason.ReplyCode == RabbitMQConstants.ConnectionForced)
                                    {
                                        break;
                                    }
                                    if (closeReason.ReplyCode == RabbitMQConstants.ChannelError)
                                    {
                                        Thread.Sleep(1000);
                                        continue;
                                    }
                                    else
                                    {
                                        throw new Exception($"{closeReason.ReplyText}");
                                    }
                                }
                                if (OnMessageReceived == null)
                                {
                                    Thread.Sleep(1000);
                                    continue;
                                }
                                if (unackCount > _prefetchCount)
                                {
                                    // 当消费堆积数，达到设定值后，将延迟1秒拉消息
                                    Thread.Sleep(1000);
                                }
                                try
                                {
                                    var mqMessage = currentChannel.BasicGet(currentQueueName, false);
                                    if (mqMessage == null)
                                    {
                                        if (noMsgCount < 1000)
                                        {
                                            // 约10秒以内
                                            Interlocked.Increment(ref noMsgCount);
                                            Thread.Sleep(10);
                                        }
                                        else if (noMsgCount < 1500)
                                        {
                                            // 约1分钟以内
                                            Interlocked.Increment(ref noMsgCount);
                                            Thread.Sleep(100);
                                        }
                                        else
                                        {
                                            // 超过1分钟
                                            Thread.Sleep(1000);
                                        }
                                        continue;
                                    }
                                    Interlocked.Exchange(ref noMsgCount, 0);
                                    Interlocked.Increment(ref unackCount);
                                    var context = new MessageHandlingTransportationContext(topic, currentQueueIndex, _groupName, currentChannel, mqMessage.DeliveryTag, new Dictionary<string, object> {
                                        { MessagePropertyConstants.MESSAGE_ID, mqMessage.BasicProperties.MessageId },
                                        { MessagePropertyConstants.MESSAGE_TYPE, mqMessage.BasicProperties.Type },
                                        { MessagePropertyConstants.TIMESTAMP, mqMessage.BasicProperties.Timestamp.UnixTime == 0 ? DateTime.Now : DateTime2UnixTime.FromUnixTime(mqMessage.BasicProperties.Timestamp.UnixTime) },
                                        { MessagePropertyConstants.CONTENT_TYPE, string.IsNullOrEmpty(mqMessage.BasicProperties.ContentType) ? "text/json" : mqMessage.BasicProperties.ContentType },
                                        { MessagePropertyConstants.BODY, mqMessage.Body },
                                        { MessagePropertyConstants.ROUTING_KEY, mqMessage.RoutingKey }
                                    });
                                    context.OnAck += (sender, e) => Interlocked.Decrement(ref unackCount);
                                    OnMessageReceived.Invoke(this, new MessageReceivedEventArgs(context));
                                }
                                catch { }
                            }
                        })
                        { IsBackground = false };
                        channelTuple = new Tuple<IModel, Thread>(channel, consumerThread);
                        consumerThread.Start(new Tuple<IModel, string, int>(channel, topic, queueIndex));
                    }
                    finally
                    {
                        channels.TryAdd(queueIndex, channelTuple);
                    }
                }
            }
        }

        private string GetSubTopic(string topic)
        {
            var groupName = string.IsNullOrEmpty(_groupName) ? "default" : _groupName;
            return $"{topic}.G.{groupName}";
        }

        private string GetQueue(string topic, int queueIndex)
        {
            return $"{GetSubTopic(topic)}-{queueIndex}";
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
