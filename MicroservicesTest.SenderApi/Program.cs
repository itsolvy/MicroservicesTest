using Hangfire;
using Hangfire.PostgreSql;
using MicroservicesTest.SenderApi.Db;
using MicroservicesTest.SenderApi.Modules;
using MicroservicesTest.SenderApi.Modules.Orders;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var featureConfig = builder.Configuration
    .GetSection(FeatureFlagsConfig.SECTION_NAME)
    .Get<FeatureFlagsConfig>() 
        ?? throw new ArgumentNullException();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    // Подключаем хранилище (в данном примере PostgreSQL)
    .UsePostgreSqlStorage(options =>
    {
        options.UseNpgsqlConnection(connectionString);
    }));

// 3. Добавляем сервер Hangfire (он будет обрабатывать задачи)
builder.Services.AddHangfireServer();

builder.Services.AddScoped<OrderModule>();
var rabbitMqHost= builder.Configuration.GetConnectionString("RabbitMq");

builder.Services.AddSingleton<RabbitMqService>( _ => new RabbitMqService(rabbitMqHost));
var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    // --- БЛОК АВТОМАТИЧЕСКОЙ МИГРАЦИИ НА СТАРТЕ ---
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        // Эта команда создаст базу данных (если её нет) и применит все недостающие миграции
        context.Database.Migrate();

        Console.WriteLine("--> Миграции успешно применены к PostgreSQL.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"--> Ошибка при применении миграций: {ex.Message}");
    }

    // --- БЛОК АВТОМАТИЧЕСКОЙ Инициализации RabbitMq ---
    try
    {

        var service = services.GetRequiredService<RabbitMqService>();

        // Эта команда создаст базу данных (если её нет) и применит все недостающие миграции
        await service.Init(featureConfig);

        Console.WriteLine("--> RabbitMq успешно инициализирован");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"--> Ошибка при инициализации RabbitMq: {ex.Message}");
    }
}


app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new AnonymousHangfireFilter() }
});

if (featureConfig.FannaotTask1Enable)
{
    app.UseMiddleware<CustomLoggingMiddleware>();
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

RecurringJob.AddOrUpdate<OrderModule>("sendToRabbit", module => module.RecurrentSend(CancellationToken.None), Cron.MinuteInterval(2));

app.Run();
