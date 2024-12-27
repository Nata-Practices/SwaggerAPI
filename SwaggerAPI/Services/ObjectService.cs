using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SwaggerAPI.Models;
using SwaggerAPI.Utils;

namespace SwaggerAPI.Services;

public interface IObjectService
{
    Task<List<ObjectModel>> GetObjectsAsync();
    Task<ObjectModel> GetObjectByIdAsync(string id);
    Task CreateObjectAsync(ObjectModel objectModel);
    Task<ObjectModel> DeleteObjectAsync(string id);
    Task UpdateConfirmationTimestampAsync(string objectId, DateTime timestamp);
}

public class ObjectService : IObjectService
{
    private readonly IMongoCollection<ObjectModel> _objects;
    private readonly IDistributedCache _cache;
    private readonly KafkaProducer _kafkaProducer;
    private readonly ILogger<ObjectService> _logger;

    public ObjectService(
        IMongoClient client,
        IOptions<MongoDBSettings> settings,
        IDistributedCache cache,
        KafkaProducer kafkaProducer,
        ILogger<ObjectService> logger)
    {
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _objects = database.GetCollection<ObjectModel>("Objects");
        _cache = cache;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    public async Task<List<ObjectModel>> GetObjectsAsync()
    {
        _logger.LogInformation("Получение всех объектов из БД.");
        return await _objects.Find(o => true).ToListAsync();
    }

    public async Task<ObjectModel> GetObjectByIdAsync(string id)
    {
        _logger.LogInformation($"Получение объекта с ID: {id}");
        var cachedObject = await _cache.GetStringAsync(id);
        if (!string.IsNullOrEmpty(cachedObject))
        {
            _logger.LogInformation($"Объект с ID: {id} найден в кэше.");
            return Newtonsoft.Json.JsonConvert.DeserializeObject<ObjectModel>(cachedObject);
        }

        var obj = await _objects.Find(o => o.Id == id).FirstOrDefaultAsync();
        if (obj != null)
        {
            _logger.LogInformation($"Объект с ID: {id} найден в БД, добавление в кэш.");
            await _cache.SetStringAsync(id, Newtonsoft.Json.JsonConvert.SerializeObject(obj));
        }
        else
        {
            _logger.LogWarning($"Объект с ID: {id} не найден.");
        }

        return obj;
    }

    public async Task CreateObjectAsync(ObjectModel objectModel)
    {
        if (string.IsNullOrEmpty(objectModel.Id))
        {
            objectModel.Id = Guid.NewGuid().ToString();
        }

        _logger.LogInformation($"Создание нового объекта: {objectModel.Name}");
        await _objects.InsertOneAsync(objectModel);
        var message = new { objectModel.Id, objectModel.UserId };
        await _kafkaProducer.SendMessageAsync(objectModel.Id, Newtonsoft.Json.JsonConvert.SerializeObject(message));
        _logger.LogInformation("Объект создан и сообщение отправлено в Kafka.");
    }

    public async Task<ObjectModel> DeleteObjectAsync(string id)
    {
        _logger.LogInformation($"Удаление объекта с ID: {id}");
        var obj = await _objects.FindOneAndDeleteAsync(o => o.Id == id);
        if (obj != null)
        {
            _logger.LogInformation($"Объект с ID: {id} успешно удален из БД.");
            await _cache.RemoveAsync(id);
            _logger.LogInformation($"Объект с ID: {id} удален из кэша.");
        }
        else
        {
            _logger.LogWarning($"Объект с ID: {id} не найден.");
        }

        return obj;
    }
    
    public async Task UpdateConfirmationTimestampAsync(string objectId, DateTime timestamp)
    {
        _logger.LogInformation($"Обновление ConfirmationTimestamp для объекта с ID={objectId}");
        var update = Builders<ObjectModel>.Update.Set(o => o.ConfirmationTimestamp, timestamp);
        var result = await _objects.UpdateOneAsync(o => o.Id == objectId, update);

        if (result.ModifiedCount > 0)
        {
            _logger.LogInformation($"Объект с ID={objectId} успешно обновлен.");
        }
        else
        {
            _logger.LogWarning($"Объект с ID={objectId} не найден для обновления.");
        }
    }
}