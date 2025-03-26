using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Text.Json;
using GameTreeVisualization.Core.Interfaces;
using GameTreeVisualization.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

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
        builder.WithOrigins("http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Game Tree API V1");
});

app.UseCors("AllowFrontend");
app.MapControllers();

app.Run("http://localhost:5002");