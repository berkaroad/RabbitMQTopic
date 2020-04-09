# RabbitMQTopic

模拟RocketMQ的Topic方式的消息发送和接收。

1）支持集群消费：相同消费组，不同消费者之间实现负载均衡，一个队列有且仅有一个消费者；

2）支持广播消费：不同消费组，可以消费同一个消息。

## 安装
```
Install-Package RabbitMQTopic
```

或

```
dotnet add package RabbitMQTopic
```

## 用法

```csharp
// 生产者
var producer = new Producer(new ProducerSettings
{
    AmqpUri = new Uri("amqp://user:pwd@ip/vhost"),
    ClientName = "YourAppName"
});

producer.Start();
var commandData = new CommandData();
var body = serializer.Serialize(commandData);
var topicMessage = new TopicMessage("CommandTopic", 4, 1, body, "text/json", commandData.GetType().FullName);
producer.SendMessage(topicMessage, "routingKey", "messageId")

producer.Shutdown();
```

```csharp
// 消费者
var consumer1 = new Consumer(new ConsumerSettings
{
    AmqpUri = new Uri("amqp://user:pwd@ip/vhost"),
    ClientName = "YourAppName",
    PrefetchCount = 10，
    GroupName = "Group1",
    ConsumerCount = 2,
    ConsumerSequence = 1 // 将消费队列 0,2
});
consumer1.OnMessageReceived += (sender, e) =>
{
    // TODO:handle message
    e.Context.Ack();
};
var consumer2 = new Consumer(new ConsumerSettings
{
    AmqpUri = new Uri("amqp://user:pwd@ip/vhost"),
    ClientName = "YourAppName",
    PrefetchCount = 10，
    GroupName = "Group1",
    ConsumerCount = 2,
    ConsumerSequence = 2 // 将消费队列 1,3
});
consumer2.OnMessageReceived += (sender, e) =>
{
    // TODO:handle message
    e.Context.Ack();
};

consumer1.Subscribe("CommandTopic", 4);
consumer2.Subscribe("CommandTopic", 4);

consumer1.Start();
consumer2.Start();

consumer1.Shutdown();
consumer2.Shutdown();
```