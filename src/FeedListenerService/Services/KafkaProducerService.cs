using System.Text.Json;
using Confluent.Kafka;

namespace FeedListenerService.Services
{
    public interface IKafkaProducerService
    {
        Task PublishAsync<T>(string topic, string key, T message);
        void Flush();
    }

    public class KafkaProducerService : IKafkaProducerService, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<KafkaProducerService> _logger;

        public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
        {
            _logger = logger;

            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "kafka:9092",
                CompressionType = CompressionType.Lz4,
                LingerMs = 10,
                BatchSize = 32768,
                Acks = Acks.All,
                EnableIdempotence = true,
                MaxInFlight = 5,
                MessageSendMaxRetries = 3
            };

            _producer = new ProducerBuilder<string, string>(config)
                .SetErrorHandler((_, e) => _logger.LogError($"Kafka error: {e.Reason}"))
                .Build();

            _logger.LogInformation("Kafka producer initialized: {BootstrapServers}", config.BootstrapServers);
        }

        public async Task PublishAsync<T>(string topic, string key, T message)
        {
            try
            {
                var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var kafkaMessage = new Message<string, string>
                {
                    Key = key,
                    Value = json,
                    Timestamp = new Timestamp(DateTime.UtcNow)
                };

                var result = await _producer.ProduceAsync(topic, kafkaMessage);

                _logger.LogDebug("Published to {Topic} [Partition {Partition}]: {Key}",
                    topic, result.Partition.Value, key);
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(ex, "Error publishing to {Topic}: {Reason}", topic, ex.Error.Reason);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error publishing to {Topic}", topic);
                throw;
            }
        }

        public void Flush()
        {
            try
            {
                _producer.Flush(TimeSpan.FromSeconds(10));
                _logger.LogInformation("Producer flushed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flushing producer");
            }
        }

        public void Dispose()
        {
            Flush();
            _producer?.Dispose();
        }
    }
}