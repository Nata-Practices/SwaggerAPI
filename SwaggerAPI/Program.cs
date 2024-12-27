using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SwaggerAPI.Services;

namespace SwaggerAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Configuration
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("Properties/appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("Properties/appsettings.Development.json", optional: true, reloadOnChange: true);
        
        // Настройка логирования
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        // Добавление контроллеров
        builder.Services.AddControllers();

        // Добавление поддержки Swagger для документации API
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Конфигурация MongoDB
        builder.Services.Configure<MongoDBSettings>(
            builder.Configuration.GetSection("MongoDB"));

        builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<MongoDBSettings>>().Value;
            return new MongoClient(settings.ConnectionString);
        });


        // Конфигурация Redis
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration.GetSection("Redis")["ConnectionString"];
        });

        // Регистрация сервисов
        builder.Services.AddSingleton<IEventService>(serviceProvider =>
        {
            var client = serviceProvider.GetRequiredService<IMongoClient>();
            var settings = serviceProvider.GetRequiredService<IOptions<MongoDBSettings>>();
            var cache = serviceProvider.GetRequiredService<IDistributedCache>();
            var logger = serviceProvider.GetRequiredService<ILogger<EventService>>();
            return new EventService(client, settings, cache, logger);
        });

        builder.Services.AddSingleton<ITicketService>(serviceProvider =>
        {
            var client = serviceProvider.GetRequiredService<IMongoClient>();
            var settings = serviceProvider.GetRequiredService<IOptions<MongoDBSettings>>();
            var cache = serviceProvider.GetRequiredService<IDistributedCache>();
            var logger = serviceProvider.GetRequiredService<ILogger<TicketService>>();
            return new TicketService(client, settings, cache, logger);
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.MapControllers(); // Регистрация контроллеров
        app.Run();
    }
}