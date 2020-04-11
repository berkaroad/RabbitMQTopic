# RabbitMQTopic

模拟RocketMQ的Topic方式的消息发送和接收。

1）支持集群消费：相同消费组，不同消费者之间实现负载均衡，一个队列有且仅有一个消费者；

2）支持广播消费：不同消费组，可以消费同一个消息；

3）支持延迟消费，需启用插件 rabbitmq_delayed_message_exchange。

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

```csharp
// 生产者
using System;
using RabbitMQTopic;

namespace Demo.ProducerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var producer = new Producer(new ProducerSettings
            {
                AmqpUri = new Uri("amqp://demo:123456@localhost/test"),
                ClientName = "ProducerApp"
            }, delayedMessageEnabled: true);

            producer.Start();
            var random = new Random();
            for (var i = 1; i <= 10; i++)
            {
                var messageId = Guid.NewGuid().ToString();
                var routingKey = Guid.NewGuid().ToString();
                var body = System.Text.Encoding.UTF8.GetBytes($"{i} delayed message {messageId}");
                var topicMessage = new TopicMessage("CommandTopic", 4, 1, body, "text/json", "System.String", delayedMillisecond: 1000 * random.Next(5, 10));
                producer.SendMessage(topicMessage, routingKey, messageId);
            }
            Console.WriteLine("Producer started!");
            Console.ReadLine();
            producer.Shutdown();
        }
    }
}
```

```csharp
// 消费者
using System;
using RabbitMQTopic;

namespace Demo.ConsumerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var consumer1 = new Consumer(new ConsumerSettings
            {
                AmqpUri = new Uri("amqp://demo:123456@localhost/test"),
                ClientName = "Consumer1App",
                PrefetchCount = 10,
                GroupName = "Group1",
                ConsumerCount = 2,
                ConsumerSequence = 1 // 将消费队列 0,2
            });
            consumer1.OnMessageReceived += (sender, e) =>
            {
                Console.WriteLine($"consumer1:{System.Text.Encoding.UTF8.GetString(e.Context.GetBody())}");
                e.Context.Ack();
            };
            var consumer2 = new Consumer(new ConsumerSettings
            {
                AmqpUri = new Uri("amqp://demo:123456@localhost/test"),
                ClientName = "Consumer2App",
                PrefetchCount = 10,
                GroupName = "Group1",
                ConsumerCount = 2,
                ConsumerSequence = 2 // 将消费队列 1,3
            });
            consumer2.OnMessageReceived += (sender, e) =>
            {
                Console.WriteLine($"consumer2:{System.Text.Encoding.UTF8.GetString(e.Context.GetBody())}");
                e.Context.Ack();
            };

            consumer1.Subscribe("CommandTopic", 4);
            consumer2.Subscribe("CommandTopic", 4);

            consumer1.Start();
            consumer2.Start();

            Console.WriteLine("Consumer started!");
            Console.ReadLine();

            consumer1.Shutdown();
            consumer2.Shutdown();
        }
    }
}
```

## 发布历史

### 1.1

1）支持延迟消费，需启用插件 rabbitmq_delayed_message_exchange；

2）Producer、Consumer初始化时，增加autoConfig参数，仅为true时才会配置Exchange、Queue和Bind。

### 1.0.1

1）支持集群消费：相同消费组，不同消费者之间实现负载均衡，一个队列有且仅有一个消费者；

2）支持广播消费：不同消费组，可以消费同一个消息。