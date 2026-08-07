using MicroservicesTest.Common;

namespace MicroservicesTest.SenderApi.Modules;

public class CustomLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RabbitMqService rabbitMqService;

    // RequestDelegate и Singleton-сервисы внедряются через конструктор
    public CustomLoggingMiddleware(RequestDelegate next, RabbitMqService rabbitMqService)
    {
        _next = next;
        this.rabbitMqService = rabbitMqService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        //_logger.LogInformation("Обработка запроса для: {Path}", context.Request.Path);

        // Передаем контекст дальше по конвейеру
        try
        {
            await rabbitMqService.SendMessageToExchange(RabbitMqConsts.LOG_EXCHANGE, context.Request.Path);
        }
        catch(Exception ex)
        {
            Console.WriteLine($"При логгировании произошла ошибка {context.Request.Path}" + ex.ToString());
        }
        await _next(context);

        //_logger.LogInformation("Обработка ответа завершена");
    }
}