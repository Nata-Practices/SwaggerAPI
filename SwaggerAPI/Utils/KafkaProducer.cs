using Confluent.Kafka;

namespace SwaggerAPI.Utils;

public class KafkaProducer
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;

    public KafkaProducer(IConfiguration configuration)
    {
        var kafkaSettings = configuration.GetSection("Kafka");
        var bootstrapServers = kafkaSettings["BootstrapServers"];
        _topic = kafkaSettings["TopicForSend"];

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task SendMessageAsync(string key, string value)
    {
        try
        {
            var message = new Message<string, string>
            {
                Key = key,
                Value = value
            };

            var result = await _producer.ProduceAsync(_topic, message);

            Console.WriteLine($"[Kafka] Сообщение отправлено в топик {_topic}: {result.TopicPartitionOffset}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Kafka] Ошибка при отправлении сообщения: {ex.Message}");
            throw;
        }
    }
}