using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SwaggerAPI.Models;

namespace SwaggerAPI.Services;

public interface ITicketService
{
    Task<List<TicketModel>> GetAllTicketsAsync();
    Task<TicketModel> GetTicketByIdAsync(string id);
    Task<TicketModel> BuyTicketAsync(TicketModel newTicket);
    Task<TicketModel> UpdateTicketAsync(string id, string buyerName);
    Task<TicketModel> ReturnTicketAsync(string id);
}

public class TicketService : ITicketService
{
    private readonly IMongoCollection<TicketModel> _tickets;
    private readonly IDistributedCache _cache;
    private readonly DistributedCacheEntryOptions _cacheOptions;
    private readonly ILogger<TicketService> _logger;

    public TicketService(IMongoClient client, IOptions<MongoDBSettings> settings, IDistributedCache cache, ILogger<TicketService> logger)
    {
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _tickets = database.GetCollection<TicketModel>("Tickets");
        _cache = cache;
        _logger = logger; // Инициализация логгера

        // Настройки кэша: время жизни 60 секунд
        _cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
        };
    }

    public async Task<List<TicketModel>> GetAllTicketsAsync()
    {
        var cacheKey = "GetAllTickets";

        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedData))
        {
            _logger.LogInformation("[TicketService] Данные получены из кэша для метода GetAllTickets.");
            return JsonSerializer.Deserialize<List<TicketModel>>(cachedData);
        }

        var tickets = await _tickets.Find(t => true).ToListAsync();

        _logger.LogInformation("[TicketService] Данные получены из MongoDB для метода GetAllTickets.");

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(tickets), _cacheOptions);

        return tickets;
    }

    public async Task<TicketModel> GetTicketByIdAsync(string id)
    {
        var cacheKey = $"GetTicketById_{id}";

        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedData))
        {
            _logger.LogInformation($"[TicketService] Данные получены из кэша для билета с Id = {id}.");
            return JsonSerializer.Deserialize<TicketModel>(cachedData);
        }

        var ticket = await _tickets.Find(t => t.Id == id).FirstOrDefaultAsync();

        if (ticket != null)
        {
            _logger.LogInformation($"[TicketService] Данные получены из MongoDB для билета с Id = {id}.");

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(ticket), _cacheOptions);
        }
        else
        {
            _logger.LogWarning($"Билет с Id = {id} не найден в MongoDB.");
        }

        return ticket;
    }

    public async Task<TicketModel> BuyTicketAsync(TicketModel newTicket)
    {
        if (string.IsNullOrEmpty(newTicket.Id))
        {
            newTicket.Id = Guid.NewGuid().ToString();
        }
        await _tickets.InsertOneAsync(newTicket);

        _logger.LogInformation($"Куплен новый билет с Id = {newTicket.Id}.");

        // Обновляем кэш
        await _cache.RemoveAsync("GetAllTickets");

        return newTicket;
    }

    public async Task<TicketModel> UpdateTicketAsync(string id, string buyerName)
    {
        var update = Builders<TicketModel>.Update.Set(t => t.BuyerName, buyerName);
        var result = await _tickets.UpdateOneAsync(t => t.Id == id, update);

        if (result.IsAcknowledged && result.ModifiedCount > 0)
        {
            _logger.LogInformation($"Билет с Id = {id} обновлен.");

            await _cache.RemoveAsync("GetAllTickets");
            await _cache.RemoveAsync($"GetTicketById_{id}");
            return await GetTicketByIdAsync(id);
        }

        _logger.LogWarning($"Не удалось обновить билет с Id = {id}.");

        return null;
    }

    public async Task<TicketModel> ReturnTicketAsync(string id)
    {
        var ticket = await GetTicketByIdAsync(id);
        if (ticket != null)
        {
            var result = await _tickets.DeleteOneAsync(t => t.Id == id);
            if (result.IsAcknowledged && result.DeletedCount > 0)
            {
                _logger.LogInformation($"Билет с Id = {id} возвращен и удален.");

                await _cache.RemoveAsync("GetAllTickets");
                await _cache.RemoveAsync($"GetTicketById_{id}");
                return ticket;
            }

            _logger.LogWarning($"Не удалось удалить билет с Id = {id}.");
        }
        else
        {
            _logger.LogWarning($"Билет с Id = {id} не найден.");
        }

        return null;
    }
}