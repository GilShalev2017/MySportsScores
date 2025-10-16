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