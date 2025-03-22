using System.Text.Json;
using GameTreeVisualization.Services;
using GameTreeVisualization.Services.Interfaces;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetValue<string>("Redis:ConnectionString")));

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
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
    });
// Services
builder.Services.AddScoped<ITreeStorageService, RedisStorageService>();
builder.Services.AddScoped<ITreeProcessingService, TreeProcessingService>();
builder.Services.AddScoped<IGameSessionService, GameSessionService>();
if (string.IsNullOrEmpty(builder.Configuration["Storage:MatchesPath"]))
{
    // Add the default path to configuration
    builder.Configuration["Storage:MatchesPath"] = "matches";
}

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

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
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