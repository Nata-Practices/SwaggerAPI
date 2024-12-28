using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using SwaggerAPI.Models;
using SwaggerAPI.Services;
using SwaggerAPI.Utils;
using Xunit;

namespace SwaggerAPI.Tests;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly KafkaProducer _producer;
    private readonly KafkaConsumer _consumer;
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerOptions options;
    
    public ApiTests(WebApplicationFactory<Program> factory)
    {
        Console.OutputEncoding = Encoding.UTF8;
        _client = factory.CreateClient();

        // Загрузка конфигурации из appsettings.json
        _configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory) // Используем базовый путь приложения
            .AddJsonFile("Properties/appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("Properties/appsettings.Development.json", optional: true, reloadOnChange: true)
            .Build();

        // Инициализация Kafka Producer и Consumer
        _producer = new KafkaProducer(_configuration);
        _consumer = new KafkaConsumer(_configuration);

        // Настраиваем JSON-сериализацию
        options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    private static void PrintMessage(string message, ConsoleColor color = ConsoleColor.White)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    [Fact]
    public async Task Add100EventsTest()
    {
        var stopwatch = Stopwatch.StartNew();
        PrintMessage("=== Тест: Добавление 100 событий ===", ConsoleColor.Cyan);

        var deleteResponse = await _client.DeleteAsync("api/events/all");
        deleteResponse.EnsureSuccessStatusCode();
        PrintMessage("Удалены все события перед началом теста.", ConsoleColor.Yellow);

        var events = new List<EventModel>();
        for (int i = 0; i < 100; i++)
        {
            events.Add(new EventModel
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"Event {i}",
                Description = $"Description {i}",
                Date = DateTime.UtcNow.AddDays(i),
                Price = i + 10
            });
        }

        foreach (var evnt in events)
        {
            var content = new StringContent(JsonSerializer.Serialize(evnt), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("api/events", content);

            var responseContent = await response.Content.ReadAsStringAsync();
            var postApiResponse = JsonSerializer.Deserialize<ApiResponse<EventModel>>(responseContent, options);

            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(postApiResponse);
            Assert.True(postApiResponse.Success);
        }

        PrintMessage("Все 100 событий успешно добавлены.", ConsoleColor.Green);

        var getResponse = await _client.GetAsync("api/events");
        getResponse.EnsureSuccessStatusCode();

        var responseContentFinal = await getResponse.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<EventModel>>>(responseContentFinal, options);

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(100, apiResponse.Data.Count);

        stopwatch.Stop();
        PrintMessage($"Тест успешно завершён. Всего событий: {apiResponse.Data.Count}. Время выполнения: {stopwatch.ElapsedMilliseconds} мс.", ConsoleColor.Magenta);
    }

    [Fact]
    public async Task Add100000EventsTest()
    {
        var stopwatch = Stopwatch.StartNew();
        PrintMessage("=== Тест: Добавление 1000 событий ===", ConsoleColor.Cyan);

        var deleteResponse = await _client.DeleteAsync("api/events/all");
        deleteResponse.EnsureSuccessStatusCode();
        PrintMessage("Удалены все события перед началом теста.", ConsoleColor.Yellow);

        var events = new List<EventModel>();
        for (int i = 0; i < 1000; i++)
        {
            events.Add(new EventModel
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"Event {i}",
                Description = $"Description {i}",
                Date = DateTime.UtcNow.AddDays(i),
                Price = i + 10
            });
        }

        foreach (var evnt in events)
        {
            var content = new StringContent(JsonSerializer.Serialize(evnt), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("api/events", content);
            response.EnsureSuccessStatusCode();
        }

        PrintMessage("Все 1000 событий успешно добавлены.", ConsoleColor.Green);

        var getResponse = await _client.GetAsync("api/events");
        getResponse.EnsureSuccessStatusCode();

        var responseContent = await getResponse.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<EventModel>>>(responseContent, options);

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(1000, apiResponse.Data.Count);

        stopwatch.Stop();
        PrintMessage($"Тест успешно завершён. Всего событий: {apiResponse.Data.Count}. Время выполнения: {stopwatch.ElapsedMilliseconds} мс.", ConsoleColor.Magenta);
    }

    [Fact]
    public async Task DeleteAllEventsTest()
    {
        var stopwatch = Stopwatch.StartNew();
        PrintMessage("=== Тест: Удаление всех событий ===", ConsoleColor.Cyan);

        var deleteResponse = await _client.DeleteAsync("api/events/all");
        deleteResponse.EnsureSuccessStatusCode();

        var deleteResponseContent = await deleteResponse.Content.ReadAsStringAsync();
        var deleteApiResponse = JsonSerializer.Deserialize<ApiResponse<string>>(deleteResponseContent, options);

        Assert.NotNull(deleteApiResponse);
        Assert.True(deleteApiResponse.Success);

        PrintMessage("Все события успешно удалены.", ConsoleColor.Green);

        var finalGetResponse = await _client.GetAsync("api/events");
        finalGetResponse.EnsureSuccessStatusCode();

        var finalResponseContent = await finalGetResponse.Content.ReadAsStringAsync();
        var finalApiResponse = JsonSerializer.Deserialize<ApiResponse<List<EventModel>>>(finalResponseContent, options);

        Assert.NotNull(finalApiResponse);
        Assert.True(finalApiResponse.Success);
        Assert.NotNull(finalApiResponse.Data);
        Assert.Empty(finalApiResponse.Data);

        stopwatch.Stop();
        PrintMessage($"Тест успешно завершён. Время выполнения: {stopwatch.ElapsedMilliseconds} мс.", ConsoleColor.Magenta);
    }
    
    [Fact]
    public async Task SendObjectToKafkaTest()
    {
        var stopwatch = Stopwatch.StartNew();
        PrintMessage("=== Тест: Отправка объекта в Kafka ===", ConsoleColor.Cyan);

        var deleteResponse = await _client.DeleteAsync("api/objects/all");
        deleteResponse.EnsureSuccessStatusCode();
        PrintMessage("Удалены все объекты перед началом теста.", ConsoleColor.Yellow);

        var newObject = new ObjectModel
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Object",
            UserId = "1"
        };
        
        var content = new StringContent(JsonSerializer.Serialize(newObject), Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("api/objects", content);

        var responseContent = await response.Content.ReadAsStringAsync();
        var postApiResponse = JsonSerializer.Deserialize<ApiResponse<ObjectModel>>(responseContent, options);

        Assert.True(response.IsSuccessStatusCode);
        Assert.NotNull(postApiResponse);
        Assert.True(postApiResponse.Success);

        PrintMessage("Объект успешно отправлен в Kafka.", ConsoleColor.Green);

        stopwatch.Stop();
        PrintMessage($"Тест завершён. Время выполнения: {stopwatch.ElapsedMilliseconds} мс.", ConsoleColor.Magenta);
    }
}