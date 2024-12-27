using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SwaggerAPI.Services;
using SwaggerAPI.Utils;

namespace SwaggerAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Меняю путь для 2-ух ебаных файлов
        builder.Configuration
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("Properties/appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("Properties/appsettings.Development.json", optional: true, reloadOnChange: true);
        
        // Выключаю автоматический трек ошибок
        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });
        
        // Настройка логирования
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        // Добавление контроллеров и подключение фильтра для валидации
        builder.Services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilter>();
        });

        // Добавление поддержки Swagger для документации API
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "API",
                Version = "v1",
                Description = "Основной API"
            });

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath);
        });

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
        app.MapControllers();
        app.Run();
    }
}