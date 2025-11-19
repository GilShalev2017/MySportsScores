using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FeedListenerService.Services;

// This file now uses the modern C# "Top-Level Statements" pattern,
// where the entry point is implicitly defined by the code at the root level of the file.

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();
builder.Services.AddHostedService<FeedGeneratorService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await EnsureTopicsExistAsync(app.Configuration);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Health Check
app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();

static async Task EnsureTopicsExistAsync(IConfiguration configuration)
{
    var bootstrapServers = configuration["Kafka:BootstrapServers"];
    var adminConfig = new AdminClientConfig { BootstrapServers = bootstrapServers };

    using var adminClient = new AdminClientBuilder(adminConfig).Build();

    var topics = new[]
    {
        new TopicSpecification { Name = "ingest-events", NumPartitions = 12, ReplicationFactor = 1 },
        new TopicSpecification { Name = "live-scores", NumPartitions = 8, ReplicationFactor = 1 },
        new TopicSpecification { Name = "player-updates", NumPartitions = 6, ReplicationFactor = 1 },
        new TopicSpecification { Name = "user-notifications", NumPartitions = 4, ReplicationFactor = 1 },
        new TopicSpecification { Name = "dead-letter-queue", NumPartitions = 2, ReplicationFactor = 1 }
    };

    try
    {
        await adminClient.CreateTopicsAsync(topics);
        Console.WriteLine("✅ Topics created with specific partitions");
    }
    catch (CreateTopicsException ex) when (ex.Results.Any(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
    {
        Console.WriteLine("ℹ️ Topics already exist");
    }
}