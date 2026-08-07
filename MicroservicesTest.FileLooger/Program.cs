// See https://aka.ms/new-console-template for more information
using MicroservicesTest.Common;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory()) // Указываем базовый путь к текущей папке
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // Читаем json
    .AddEnvironmentVariables()
    .Build();

var hostUri = configuration["ConnectionStrings:RabbitMq"] ?? throw new ArgumentNullException();
var fileName = $"{DateTime.Now.ToString("yyyy.MM.dd_HH:mm:ss")}_log.txt".Replace(" ", "_");
var factory = new ConnectionFactory() { Uri = new Uri(hostUri) };
using (var connection = await factory.CreateConnectionAsync())
using (var channel = await connection.CreateChannelAsync())
{
    var consumer = new AsyncEventingBasicConsumer(channel);

    consumer.ReceivedAsync += async (sender, ea) =>
    {
        try
        {
            var body = ea.Body.ToArray();
            var messageText = Encoding.UTF8.GetString(body);
            File.AppendAllLines(fileName, [messageText]);

            await Task.Delay(500); // Имитация работы

            await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
        }
    };

    await channel.BasicConsumeAsync(queue: RabbitMqConsts.LOG_QUEQUE_2, autoAck: false, consumer: consumer);


    // 1. Создаем источник токена отмены
    var cts = new CancellationTokenSource();

    // 2. Подписываемся на системные события закрытия контейнера (Docker stop)
    AppDomain.CurrentDomain.ProcessExit += (s, e) => cts.Cancel();
    Console.CancelKeyPress += (s, e) =>
    {
        e.Cancel = true; // предотвращаем мгновенное жесткое убийство процесса
        cts.Cancel();
    };

    try
    {
        await Task.Delay(Timeout.Infinite, cts.Token);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Получен сигнал остановки. Завершаем работу...");
    }
}