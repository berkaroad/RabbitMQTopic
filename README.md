# RabbitMQTopic

模拟RocketMQ的Topic方式的消息发送和接收。

1）支持集群消费：相同消费组，不同消费者之间实现负载均衡，一个队列有且仅有一个消费者；

2）支持广播消费：不同消费组，可以消费同一个消息；

3）支持延迟消息，需启用插件 rabbitmq_delayed_message_exchange。

## 安装
```
Install-Package RabbitMQTopic
```

或

```
dotnet add package RabbitMQTopic
```

## Topic 与 RabbitMQ 的映射关系

Consumer 启动后，如果设置的autoConfig为true，则会自动创建Exchange、Queue和Bind；

Producer 启动后，如果设置的autoConfig为true，则会自动创建Exchange和Bind。

每创建一个Topic，对应在RabbitMQ下会创建如下：

1）一个Topic对应一个Exchange（fanout）

```csharp
ExchangeDeclare("<TopicName>", "fanout", true, false);
```

2）Topic下，一个消费组对应一个Exchange（direct），默认消费组名为“default”

```csharp
ExchangeDeclare("<TopicName>.G.<GroupName>", "direct", true, false);
```

3）Topic下，一个消费组对应的Queue数为Topic的队列数，如Topic的队列数为4，则Queue如下

```csharp
QueueDeclare("<TopicName>.G.<GroupName>-0", true, false, false);
QueueDeclare("<TopicName>.G.<GroupName>-1", true, false, false);
QueueDeclare("<TopicName>.G.<GroupName>-2", true, false, false);
QueueDeclare("<TopicName>.G.<GroupName>-3", true, false, false);
```

4）添加Exchange绑定

```csharp
ExchangeBind("<TopicName>.G.<GroupName>", "<TopicName>", "");
```

5）添加Queue绑定

```csharp
QueueBind("<TopicName>.G.<GroupName>-0", "<TopicName>.G.<GroupName>", "0");
QueueBind("<TopicName>.G.<GroupName>-1", "<TopicName>.G.<GroupName>", "1");
QueueBind("<TopicName>.G.<GroupName>-2", "<TopicName>.G.<GroupName>", "2");
QueueBind("<TopicName>.G.<GroupName>-3", "<TopicName>.G.<GroupName>", "3");
```

6）在发送延迟消息前，会额外创建Exchange（x-delayed-message）和Bind。（RabbitMQ需启用插件 rabbitmq_delayed_message_exchange）

```csharp
ExchangeDeclare("<TopicName>-delayed", "x-delayed-message", true, false, new Dictionary<string, object> {
    { "x-delayed-type", "fanout" }
});
ExchangeBind("<TopicName>", "<TopicName>-delayed", "");
```

## 用法

消费者用法，见[Demo.ConsumerApp]("https://github.com/berkaroad/RabbitMQTopic/tree/master/src/Demo.ConsumerApp")

生产者用法，见[Demo.Producer]("https://github.com/berkaroad/RabbitMQTopic/tree/master/src/Demo.ProducerApp")

## 发布历史

### 1.1.1

1）支持延迟消息，需启用插件 rabbitmq_delayed_message_exchange；

2）Producer、Consumer初始化时，增加autoConfig参数，仅为true时才会配置Exchange、Queue和Bind。

### 1.0.1

1）支持集群消费：相同消费组，不同消费者之间实现负载均衡，一个队列有且仅有一个消费者；

2）支持广播消费：不同消费组，可以消费同一个消息。