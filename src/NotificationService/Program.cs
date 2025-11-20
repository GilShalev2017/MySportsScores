using Common.Data;
using Microsoft.EntityFrameworkCore;
using NotificationService.Hubs;
using NotificationService.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add Redis for SignalR backplane
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")));

// Add SignalR with Redis backplane
// FIX: Using AddStackExchangeRedis() method which is provided by the modern
// Microsoft.AspNetCore.SignalR.StackExchangeRedis NuGet package (v8.0.0 for .NET 8)
builder.Services.AddSignalR()
    .AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis"), options =>
    {
        options.Configuration.ChannelPrefix = "SignalR";
    });

// Add Services
// Add DbContext for user preferences
builder.Services.AddDbContext<SportsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

// Register UserPreferenceService (SQL-backed)
builder.Services.AddScoped<IUserPreferenceService, UserPreferenceService>(); 

builder.Services.AddHostedService<KafkaNotificationConsumerService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseRouting();

app.MapHub<SportsNotificationHub>("/sportshub");
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "NotificationService" }));

app.Run();
