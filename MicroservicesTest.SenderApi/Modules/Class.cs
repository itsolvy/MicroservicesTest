using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace MicroservicesTest.SenderApi.Modules;

public class RabbitMqService(string uri)
{

    public Task SendMessage(object obj)
    {
        var message = JsonSerializer.Serialize(obj);
        return SendMessage(message);
    }

    public async Task SendMessage(string message)
    {
        // Не забудьте вынести значения "localhost" и "MyQueue"
        // в файл конфигурации
        var factory = new ConnectionFactory() { Uri = new Uri(uri) };
        using (var connection = await factory.CreateConnectionAsync())
        using (var channel = await connection.CreateChannelAsync())
        {
            await channel.QueueDeclareAsync(
                queue: "MyQueue",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var body = Encoding.UTF8.GetBytes(message);

            var properties = new BasicProperties
            {
                DeliveryMode = DeliveryModes.Persistent // Сохранять на диск
            };

            await channel.BasicPublishAsync(
                 exchange: "",
                 routingKey: "MyQueue",
                 mandatory: false,
                 basicProperties: properties,
                 body: body);
        }
    }
}
