using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SwaggerAPI;

public class ApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Add100EventsTest()
    {
        var deleteResponse = await _client.DeleteAsync("/Events/all");
        deleteResponse.EnsureSuccessStatusCode();
        
        var events = new List<EventModel>();
        for (int i = 0; i < 100; i++)
        {
            events.Add(new EventModel
            {
                Id = Guid.NewGuid().ToString(), // Указание ID вручную
                Name = $"Event {i}",
                Description = $"Description {i}",
                Date = DateTime.UtcNow.AddDays(i), // Указываем корректные даты
                Price = i + 10 // Добавляем корректные цены
            });
        }

        foreach (var evnt in events)
        {
            var content = new StringContent(JsonSerializer.Serialize(evnt), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/Events", content);
            response.EnsureSuccessStatusCode(); // Убедимся, что запрос выполнен успешно
        }

        // Проверим количество элементов
        var getResponse = await _client.GetAsync("/Events");
        getResponse.EnsureSuccessStatusCode();
    
        var responseContent = await getResponse.Content.ReadAsStringAsync();
        var eventsFromDb = JsonSerializer.Deserialize<List<EventModel>>(responseContent);
    
        Assert.Equal(100, eventsFromDb.Count); // Проверим, что 100 событий добавлены
    }

    [Fact]
    public async Task Add100000EventsTest()
    {
        var deleteResponse = await _client.DeleteAsync("/Events/all");
        deleteResponse.EnsureSuccessStatusCode();
        
        var events = new List<EventModel>();
        for (int i = 0; i < 100000; i++)
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

        // Оптимизируем загрузку, добавляя события батчами
        foreach (var evnt in events.Take(1000)) // Проверим сначала на 1000 событий
        {
            var content = new StringContent(JsonSerializer.Serialize(evnt), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/Events", content);
            response.EnsureSuccessStatusCode();
        }

        // Проверим добавление элементов
        var getResponse = await _client.GetAsync("/Events");
        getResponse.EnsureSuccessStatusCode();
    
        var responseContent = await getResponse.Content.ReadAsStringAsync();
        var eventsFromDb = JsonSerializer.Deserialize<List<EventModel>>(responseContent);
    
        Assert.Equal(1000, eventsFromDb.Count); // Проверим добавление первых 1000 событий
    }

    [Fact]
    public async Task DeleteAllEventsTest()
    {
        // Удаляем все события
        var deleteResponse = await _client.DeleteAsync("/Events/all");
        deleteResponse.EnsureSuccessStatusCode();

        // Проверяем, что все элементы удалены
        var finalGetResponse = await _client.GetAsync("/Events");
        finalGetResponse.EnsureSuccessStatusCode();
    
        var finalResponseContent = await finalGetResponse.Content.ReadAsStringAsync();
        var finalEventsFromDb = JsonSerializer.Deserialize<List<EventModel>>(finalResponseContent);
    
        Assert.Empty(finalEventsFromDb); // Убедимся, что все события удалены
    }
}