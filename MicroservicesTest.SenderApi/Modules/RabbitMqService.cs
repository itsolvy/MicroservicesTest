using MicroservicesTest.Common;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace MicroservicesTest.SenderApi.Modules;

public class RabbitMqService : IAsyncDisposable, IDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly Task<IConnection> _connectionTask;

    public RabbitMqService(string uri)
    {
        _factory = new ConnectionFactory()
        {
            Uri = new Uri(uri),
            ConsumerDispatchConcurrency=10
        };
        _connectionTask = _factory.CreateConnectionAsyncRetry();
    }

    public async ValueTask DisposeAsync()
    {
        var connection = await _connectionTask;
        connection.Dispose();
    }

    public void Dispose()
    {
        var connection = _connectionTask.GetAwaiter().GetResult();
        if (connection != null)
        {
            // Поскольку IConnection в v7+ асинхронный, 
            // в синхронном методе приходится блокировать поток через .GetAwaiter().GetResult()
            connection.DisposeAsync().GetAwaiter().GetResult();
            connection = null;
        }

        GC.SuppressFinalize(this);
    }

    public async Task Init(FeatureFlagsConfig config)
    {
        var connection = await _connectionTask;
        await using var channel = await connection.CreateChannelAsync();


        await channel.QueueDeclareAsync(
                queue: RabbitMqConsts.ORDERS_QUEQUE,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

        if (config.FannaotTask1Enable)
        {
            await channel.ExchangeDeclareAsync(
                RabbitMqConsts.LOG_EXCHANGE,
                ExchangeType.Fanout,
                durable: true);

            await channel.QueueDeclareAsync(
                queue: RabbitMqConsts.LOG_QUEQUE_1,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            await channel.QueueDeclareAsync(
                queue: RabbitMqConsts.LOG_QUEQUE_2,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            await channel.QueueBindAsync(
                RabbitMqConsts.LOG_QUEQUE_1,
                RabbitMqConsts.LOG_EXCHANGE,
                "");

            await channel.QueueBindAsync(
                RabbitMqConsts.LOG_QUEQUE_2,
                RabbitMqConsts.LOG_EXCHANGE,
                "");


        }
        ;
    }



    public Task SendMessageDirect(string quequeName, object obj)
    {
        var message = JsonSerializer.Serialize(obj);
        return SendMessageDirect(quequeName, message);
    }


    public async Task SendMessageDirect(string quequeName, string message)
    {
        // Не забудьте вынести значения "localhost" и "MyQueue"
        // в файл конфигурации
        //var factory = new ConnectionFactory() { Uri = new Uri(uri) };
        var connection = await _connectionTask;
        await using var channel = await connection.CreateChannelAsync();

        var body = Encoding.UTF8.GetBytes(message);

        var properties = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent // Сохранять на диск
        };

        await channel.BasicPublishAsync(
             exchange: "",
             routingKey: quequeName,
             mandatory: false,
             basicProperties: properties,
             body: body);

    }

    public async Task SendMessageToExchange(string exchange, string message)
    {
        var connection = await _connectionTask;
        await using var channel = await connection.CreateChannelAsync();

        var body = Encoding.UTF8.GetBytes(message);

        var properties = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent // Сохранять на диск
        };

        await channel.BasicPublishAsync(
             exchange: exchange,
             routingKey: "asd",
             mandatory: false,
             basicProperties: properties,
             body: body);

    }
}
