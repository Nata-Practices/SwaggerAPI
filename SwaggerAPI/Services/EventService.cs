using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SwaggerAPI.Models;

namespace SwaggerAPI.Services;

public interface IEventService
{
    Task<List<EventModel>> GetAllEventsAsync();
    Task<EventModel> GetEventByIdAsync(string id);
    Task<EventModel> AddEventAsync(EventModel newEvent);
    Task<EventModel> UpdateEventAsync(string id, EventModel updatedEvent);
    Task<EventModel> DeleteEventAsync(string id);
}

public class EventService : IEventService
{
    private readonly IMongoCollection<EventModel> _events;
    private readonly IDistributedCache _cache;
    private readonly DistributedCacheEntryOptions _cacheOptions;
    private readonly ILogger<EventService> _logger;

    public EventService(IMongoClient client, IOptions<MongoDBSettings> settings, IDistributedCache cache, ILogger<EventService> logger)
    {
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _events = database.GetCollection<EventModel>("Events");
        _cache = cache;
        _logger = logger; // Инициализация логгера

        // Настройки кэша: время жизни 60 секунд
        _cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
        };
    }

    public async Task<List<EventModel>> GetAllEventsAsync()
    {
        var cacheKey = "GetAllEvents";

        // Попытка получить данные из кэша
        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedData))
        {
            _logger.LogInformation("[EventService] Данные получены из кэша для метода GetAllEvents.");
            return JsonSerializer.Deserialize<List<EventModel>>(cachedData);
        }

        // Если в кэше нет, получаем из базы данных
        var events = await _events.Find(e => true).ToListAsync();

        _logger.LogInformation("[EventService] Данные получены из MongoDB для метода GetAllEvents.");

        // Сохраняем в кэш
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(events), _cacheOptions);

        return events;
    }

    public async Task<EventModel> GetEventByIdAsync(string id)
    {
        var cacheKey = $"GetEventById_{id}";

        // Попытка получить данные из кэша
        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedData))
        {
            _logger.LogInformation($"[EventService] Данные получены из кэша для события с Id = {id}.");
            return JsonSerializer.Deserialize<EventModel>(cachedData);
        }

        // Если в кэше нет, получаем из базы данных
        var evnt = await _events.Find(e => e.Id == id).FirstOrDefaultAsync();

        if (evnt != null)
        {
            _logger.LogInformation($"[EventService] Данные получены из MongoDB для события с Id = {id}.");

            // Сохраняем в кэш
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(evnt), _cacheOptions);
        }
        else
        {
            _logger.LogWarning($"Событие с Id = {id} не найдено в MongoDB.");
        }

        return evnt;
    }

    public async Task<EventModel> AddEventAsync(EventModel newEvent)
    {
        if (string.IsNullOrEmpty(newEvent.Id))
        {
            newEvent.Id = Guid.NewGuid().ToString();
        }
        await _events.InsertOneAsync(newEvent);

        _logger.LogInformation($"Добавлено новое событие с Id = {newEvent.Id}.");

        // Очистка кэша списка событий, так как данные изменились
        await _cache.RemoveAsync("GetAllEvents");

        return newEvent;
    }

    public async Task<EventModel> UpdateEventAsync(string id, EventModel updatedEvent)
    {
        updatedEvent.Id = id;
        var result = await _events.ReplaceOneAsync(e => e.Id == id, updatedEvent);

        if (result.IsAcknowledged && result.ModifiedCount > 0)
        {
            _logger.LogInformation($"Событие с Id = {id} обновлено.");

            // Обновляем кэш
            await _cache.RemoveAsync("GetAllEvents");
            await _cache.RemoveAsync($"GetEventById_{id}");
            return updatedEvent;
        }

        _logger.LogWarning($"Не удалось обновить событие с Id = {id}.");

        return null;
    }

    public async Task<EventModel> DeleteEventAsync(string id)
    {
        var result = await _events.DeleteOneAsync(e => e.Id == id);

        if (result.IsAcknowledged && result.DeletedCount > 0)
        {
            _logger.LogInformation($"Событие с Id = {id} удалено.");

            // Обновляем кэш
            await _cache.RemoveAsync("GetAllEvents");
            await _cache.RemoveAsync($"GetEventById_{id}");
            return new EventModel { Id = id };
        }

        _logger.LogWarning($"Не удалось удалить событие с Id = {id}.");

        return null;
    }
}