# RabbitMQTopic

模拟RocketMQ的Topic方式的消息发送和接收。

1）支持集群消费：相同消费组，不同消费者之间实现负载均衡，一个队列有且仅有一个消费者；

2）支持广播消费：不同消费组，可以消费同一个消息；

3）支持延迟消息，需启用插件 rabbitmq_delayed_message_exchange；

4）消费模式，同时支持Push模式和Pull模式（Pull模式下，未响应数超过此设置后，将暂停1秒拉取消息）。

## 安装
```
Install-Package RabbitMQTopic
```

或

```
dotnet add package RabbitMQTopic
```

## 用法

消费者用法，见 [ConsumerApp](src/Samples/ConsumerApp/Program.cs)

生产者用法，见 [ProducerApp](src/Samples/ProducerApp/Program.cs)

## 性能测试

性能测试，见 [PerformanceTests](src/Samples/PerformanceTests/Program.cs)

以下数据，是在双核Mac笔记本上进行，dotnetcore和rabbitmq都在笔记本上，CPU消耗各占一半。

```
Send message completed, time spent: 110272ms, message count: 100000, throughput: 906tps.

Consume message by push completed, time spent: 4399ms, message count: 52329, throughput: 11895tps.

Consume message by pull completed, time spent: 8023ms, message count: 47674, throughput: 5942tps.
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

## 发布历史

### 1.2.1
1）优化Pull模式，在空转情况下，平衡实时性和空转性能消耗。

### 1.2.0
1）TopicMessage更名为Message，取消属性QueueCount，更名DelayedMillisecond为DelayedMilliseconds；

2）Producer启动前，需先注册Topic，仅注册过Topic的才可以发送消息；

3）优化Producer配置Topic的逻辑，由原先伴随SendMessage，改为Start的时候；

4）Producer发送消息时，增加返回类型SendResult；

5）ProducerSettings，增加配置SendMsgTimeout，用于发送确认超时设置，默认3秒；

6）增加Producer和Consumer是否运行的逻辑验证；

7）路由Hash算法，改为Crc16算法；

8）IMessageTransportationContext 属性ExchangeName改为Topic，属性QueueName改为QueueIndex；

9）增加Pull模式。

### 1.1.3

1）支持延迟消息，需启用插件 rabbitmq_delayed_message_exchange；

2）Producer、Consumer初始化时，增加autoConfig参数，仅为true时才会配置Exchange、Queue和Bind。

### 1.0.1

1）支持集群消费：相同消费组，不同消费者之间实现负载均衡，一个队列有且仅有一个消费者；

2）支持广播消费：不同消费组，可以消费同一个消息。