using System.Text.Json;
using GameTreeVisualization.Services.Interfaces;
using StackExchange.Redis;

namespace GameTreeVisualization.Services;

public class RedisStorageService : ITreeStorageService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisStorageService> _logger;
    private const string KeyPrefix = "tree:";
    private readonly int _expirationMinutes;

    public RedisStorageService(
        IConnectionMultiplexer redis, 
        IConfiguration configuration,
        ILogger<RedisStorageService> logger)
    {
        _redis = redis;
        _logger = logger;
        _expirationMinutes = configuration.GetValue<int>("CacheSettings:TreeDataExpirationMinutes");
    }

    public async Task<TreeNode> GetStoredTree()
    {
        var db = _redis.GetDatabase();
        var data = await db.StringGetAsync(KeyPrefix + "current");
        
        if (data.IsNull)
            throw new InvalidOperationException("No tree data available");

        return JsonSerializer.Deserialize<TreeNode>(data);
    }

    public async Task StoreTree(TreeNode tree)
    {
        var db = _redis.GetDatabase();
        var data = JsonSerializer.Serialize(tree);
        await db.StringSetAsync(
            KeyPrefix + "current", 
            data,
            TimeSpan.FromMinutes(_expirationMinutes)
        );
    }

    public async Task<bool> TreeExists()
    {
        var db = _redis.GetDatabase();
        return await db.KeyExistsAsync(KeyPrefix + "current");
    }

    public async Task ClearStorage()
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(KeyPrefix + "current");
    }
}