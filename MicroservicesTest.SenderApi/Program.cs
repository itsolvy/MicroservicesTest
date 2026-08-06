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

builder.Services.AddScoped<RabbitMqService>( _ => new RabbitMqService(rabbitMqHost));
var app = builder.Build();


// --- БЛОК АВТОМАТИЧЕСКОЙ МИГРАЦИИ НА СТАРТЕ ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
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
}
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new AnonymousHangfireFilter() }
});

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
