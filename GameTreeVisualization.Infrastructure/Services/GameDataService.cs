using System.Text.RegularExpressions;
using GameTreeVisualization.Core.Interfaces;
using GameTreeVisualization.Core.Models.Tree;
using GameTreeVisualization.Infrastructure.Models.Redis;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace GameTreeVisualization.Infrastructure.Services;

public class GameDataService : IGameDataService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly TreeMapper _treeMapper;
    private readonly ILogger<GameDataService> _logger;
    private const string RedisKeyPrefix = "mcts:";

    public GameDataService(
        IConnectionMultiplexer redis,
        TreeMapper treeMapper,
        ILogger<GameDataService> logger)
    {
        _redis = redis;
        _treeMapper = treeMapper;
        _logger = logger;
    } 

    public async Task<TreeNode> GetTreeForSession(string sessionId, int turnNumber)
    {
        var db = _redis.GetDatabase();
        var turnId = turnNumber.ToString("D3");
        var finalKey = $"{RedisKeyPrefix}{sessionId}:{turnId}:final";

        var treeData = await db.StringGetAsync(finalKey);
        if (treeData.IsNull)
            throw new InvalidOperationException($"Нет данных для сессии {sessionId} и хода {turnNumber}");

        var redisTree = JsonConvert.DeserializeObject<RedisTree>(treeData);
        return _treeMapper.MapToTreeNode(redisTree.Root);
    }

    public bool SessionExists(string sessionId)
    {
        try 
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var matchingKeys = server.Keys(pattern: $"{RedisKeyPrefix}{sessionId}*").ToArray();
            return matchingKeys.Length > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка проверки сессии {sessionId}");
            return false;
        }
    }

    public List<int> GetAvailableTurnsForSession(string sessionId)
    {
        try 
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            
            // Получение ключей финальных состояний
            var finalTurnKeys = server.Keys(pattern: $"{RedisKeyPrefix}{sessionId}:*:final")
                .Select(key => ExtractTurnNumber(key.ToString(), "final"))
                .ToList();

            // Если нет финальных, ищем ключи роста
            if (!finalTurnKeys.Any())
            {
                finalTurnKeys = server.Keys(pattern: $"{RedisKeyPrefix}{sessionId}:*:growth_*")
                    .Select(key => ExtractTurnNumber(key.ToString(), "growth_"))
                    .Distinct()
                    .ToList();
            }

            return finalTurnKeys.OrderBy(t => t).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка получения доступных ходов для сессии {sessionId}");
            throw;
        }
    }

    private int ExtractTurnNumber(string key, string pattern)
    {
        var regex = new Regex($@"{RedisKeyPrefix}[^:]+:(\d+):{pattern}");
        var match = regex.Match(key);
        return match.Success ? int.Parse(match.Groups[1].Value) : -1;
    }

    public async Task<List<TreeGrowthStep>> GetTreeGrowthSteps(string sessionId, int turnNumber)
    {
        var db = _redis.GetDatabase();
        var turnId = turnNumber.ToString("D3");
        var growthSteps = new List<TreeGrowthStep>();

        try 
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var growthKeys = server.Keys(pattern: $"{RedisKeyPrefix}{sessionId}:{turnId}:growth_*")
                .Select(k => k.ToString())
                .OrderBy(ExtractGrowthNumber)
                .ToList();
            
            growthKeys.Add(server.Keys(pattern: $"{RedisKeyPrefix}{sessionId}:{turnId}:final").First().ToString());

            int stepNumber = 0;
            foreach (var key in growthKeys)
            {
                var redisTree = await db.StringGetAsync(key);
                if (redisTree.HasValue)
                {
                    var redisTreeObj = JsonConvert.DeserializeObject<RedisTree>(redisTree);
                    var tree = _treeMapper.MapToTreeNode(redisTreeObj.Root);

                    growthSteps.Add(new TreeGrowthStep
                    {
                        StepNumber = stepNumber++,
                        Turn = turnNumber,
                        PatchNumber = ExtractGrowthNumber(key) is var num && num == -1 ? "final" : num.ToString(),
                        Tree = tree
                    });
                }
            }

            return growthSteps;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка получения шагов роста для сессии {sessionId}");
            throw;
        }
    }

    private int ExtractGrowthNumber(string key)
    {
        var match = Regex.Match(key, @"growth_(\d+)$");
        return match.Success ? int.Parse(match.Groups[1].Value) : -1;
    }
}