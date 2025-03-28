using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Text.Json;
using GameTreeVisualization.Core.Interfaces;
using GameTreeVisualization.Infrastructure.Services;
using GameTreeVisualization.Web.Middleware;
using Microsoft.IO;

var builder = WebApplication.CreateBuilder(args);

// Add the Microsoft.IO.RecyclableMemoryStream package to manage memory more efficiently
builder.Services.AddSingleton<RecyclableMemoryStreamManager>();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetValue<string>("Redis:ConnectionString")));

// Mappers
builder.Services.AddSingleton<TreeMapper>();

builder.Services.AddScoped<IGameDataService, GameDataService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Game Tree Visualization API", 
        Version = "v1",
        Description = "API for game tree visualization"
    });
});

// JSON Options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = 
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", builder =>
    {
        builder
            .AllowAnyOrigin()     // Allow any origin
            .AllowAnyMethod()     // Allow any HTTP method 
            .AllowAnyHeader();    // Allow any HTTP headers
    });
});

var app = builder.Build();

// Add custom request/response logging middleware
app.UseRequestResponseLogging();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Game Tree API V1");
});

app.UseCors("AllowFrontend");
app.MapControllers();

// Enable logging the application startup
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Game Tree Visualization API starting...");
logger.LogInformation($"Environment: {app.Environment.EnvironmentName}");
logger.LogInformation($"Redis connection: {builder.Configuration.GetValue<string>("Redis:ConnectionString")}");

app.Run();