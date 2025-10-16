namespace Scores365.IngestService.Services;

using Common.Events;
using Confluent.Kafka;
using global::IngestService.Services;
using System.Text.Json;

public class KafkaConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly List<IConsumer<string, string>> _consumers = new();
    private long _totalMessagesProcessed = 0;

    public KafkaConsumerService(
        IServiceProvider serviceProvider,
        ILogger<KafkaConsumerService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("KafkaConsumerService started");

        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"],
            GroupId = "ingest-service-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,
            MaxPollIntervalMs = 300000,
            SessionTimeoutMs = 45000
        };

        // Start multiple consumer tasks for different topics
        var consumerTasks = new List<Task>
        {
            ConsumeTopicAsync("ingest-events", ProcessSportEventAsync, config, stoppingToken),
            ConsumeTopicAsync("live-scores", ProcessScoreUpdateAsync, config, stoppingToken),
            ConsumeTopicAsync("player-updates", ProcessPlayerUpdateAsync, config, stoppingToken)
        };

        // Start metrics reporting
        _ = Task.Run(() => ReportMetrics(stoppingToken), stoppingToken);

        await Task.WhenAll(consumerTasks);
    }

    private async Task ConsumeTopicAsync<T>(
        string topic,
        Func<T, Task> processFunc,
        ConsumerConfig config,
        CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig(config)
        {
            GroupId = $"{config.GroupId}-{topic}"
        };

        var consumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka error on {Topic}: {Reason}", topic, e.Reason))
            .Build();

        _consumers.Add(consumer);
        consumer.Subscribe(topic);

        _logger.LogInformation("Subscribed to topic: {Topic}", topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));
                    
                    if (consumeResult != null)
                    {
                        await ProcessMessageAsync(consumeResult, processFunc);
                        
                        // Commit offset after successful processing
                        consumer.Commit(consumeResult);
                        consumer.StoreOffset(consumeResult);
                        
                        Interlocked.Increment(ref _totalMessagesProcessed);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Consume error on {Topic}", topic);
                    
                    // Send to DLQ if needed
                    if (ex.Error.IsFatal)
                    {
                        await SendToDeadLetterQueueAsync(topic, ex.ConsumerRecord?.Message?.Value, ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from {Topic}", topic);
                }
            }
        }
        finally
        {
            consumer.Close();
            consumer.Dispose();
        }
    }

    private async Task ProcessMessageAsync<T>(
        ConsumeResult<string, string> consumeResult,
        Func<T, Task> processFunc)
    {
        try
        {
            var message = JsonSerializer.Deserialize<T>(consumeResult.Message.Value, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (message != null)
            {
                using var scope = _serviceProvider.CreateScope();
                await processFunc(message);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for message: {Message}", consumeResult.Message.Value);
            await SendToDeadLetterQueueAsync(consumeResult.Topic, consumeResult.Message.Value, "Deserialization failed");
        }
    }

    private async Task ProcessSportEventAsync(SportEventUpdate eventUpdate)
    {
        using var scope = _serviceProvider.CreateScope();
        var ingestionService = scope.ServiceProvider.GetRequiredService<IDataIngestionService>();
        await ingestionService.ProcessSportEventAsync(eventUpdate);
    }

    private async Task ProcessScoreUpdateAsync(ScoreUpdate scoreUpdate)
    {
        using var scope = _serviceProvider.CreateScope();
        var ingestionService = scope.ServiceProvider.GetRequiredService<IDataIngestionService>();
        await ingestionService.ProcessScoreUpdateAsync(scoreUpdate);
    }

    private async Task ProcessPlayerUpdateAsync(PlayerUpdate playerUpdate)
    {
        using var scope = _serviceProvider.CreateScope();
        var ingestionService = scope.ServiceProvider.GetRequiredService<IDataIngestionService>();
        await ingestionService.ProcessPlayerUpdateAsync(playerUpdate);
    }

    private async Task SendToDeadLetterQueueAsync(string originalTopic, string message, string error)
    {
        try
        {
            var dlqConfig = new ProducerConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"]
            };

            using var producer = new ProducerBuilder<string, string>(dlqConfig).Build();
            
            var dlqMessage = new
            {
                originalTopic,
                error,
                message,
                timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(dlqMessage);
            await producer.ProduceAsync("dead-letter-queue", new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = json
            });

            _logger.LogWarning("Sent message to DLQ from topic {Topic}: {Error}", originalTopic, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to DLQ");
        }
    }

    private async Task ReportMetrics(CancellationToken stoppingToken)
    {
        var lastCount = 0L;
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(10000, stoppingToken);
            
            var currentCount = Interlocked.Read(ref _totalMessagesProcessed);
            var messagesPerSecond = (currentCount - lastCount) / 10.0;
            lastCount = currentCount;
            
            _logger.LogInformation(
                "📊 IngestService Metrics - Total Processed: {TotalMessages:N0} | Messages/sec: {MessagesPerSec:N1}", 
                currentCount, 
                messagesPerSecond);
        }
    }

    public override void Dispose()
    {
        foreach (var consumer in _consumers)
        {
            consumer?.Close();
            consumer?.Dispose();
        }
        base.Dispose();
    }
}