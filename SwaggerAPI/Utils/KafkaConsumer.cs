using Confluent.Kafka;

namespace SwaggerAPI.Utils;

public class KafkaConsumer
{
    private readonly IConsumer<string, string> _consumer;
    private readonly string _topic;

    public KafkaConsumer(IConfiguration configuration)
    {
        var kafkaSettings = configuration.GetSection("Kafka");
        var bootstrapServers = kafkaSettings["BootstrapServers"];
        var groupId = kafkaSettings["GroupId"];
        _topic = kafkaSettings["TopicForListen"];

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    public void StartListening(Action<string, string> handleMessage)
    {
        _consumer.Subscribe(_topic);

        Task.Run(() =>
        {
            try
            {
                while (true)
                {
                    var result = _consumer.Consume();
                    handleMessage(result.Key, result.Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Kafka] Ошибка приёма сообщения: {ex.Message}");
            }
        });
    }
}