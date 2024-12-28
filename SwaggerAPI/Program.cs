using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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
        
        // Добавлем JWT
        var jwtSettings = builder.Configuration.GetSection("Jwt");
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var secretKey = jwtSettings["SecretKey"];

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                };
            });
        
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
            
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Введите JWT токен с префиксом 'Bearer '",
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
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
        
        // Конфигурация kafka
        builder.Services.AddSingleton<KafkaProducer>();
        builder.Services.AddSingleton<KafkaConsumer>();
        
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
        
        builder.Services.AddSingleton<IObjectService>(serviceProvider =>
        {
            var client = serviceProvider.GetRequiredService<IMongoClient>();
            var settings = serviceProvider.GetRequiredService<IOptions<MongoDBSettings>>();
            var cache = serviceProvider.GetRequiredService<IDistributedCache>();
            var logger = serviceProvider.GetRequiredService<ILogger<ObjectService>>();
            var kafkaProducer = serviceProvider.GetRequiredService<KafkaProducer>();
            return new ObjectService(client, settings, cache, kafkaProducer, logger);
        });
        
        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        
        // Регистрируем приём сообщений от kafka
        var kafkaConsumer = app.Services.GetRequiredService<KafkaConsumer>();
        kafkaConsumer.StartListening(async (key, value) =>
        {
            Console.WriteLine($"[Kafka] Новое сообщение: Key={key}, Value={value}");
            var objectService = app.Services.GetRequiredService<IObjectService>();

            try
            {
                await objectService.UpdateConfirmationTimestampAsync(key, DateTime.Parse(value));
                Console.WriteLine($"[Kafka] Объект с ID={key} обновлен: ConfirmationTimestamp={value}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Kafka] Ошибка обновления объекта: {ex.Message}");
            }
        });
        
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseHttpsRedirection();
        app.MapControllers();
        app.Run();
    }
}