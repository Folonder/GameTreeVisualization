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
            var growthKeys = server.Keys(pattern: $"{RedisKeyPrefix}{sessionId}:{turnId}:growth_*:tree")
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

    public async Task<IterationDetails> GetIterationDetails(string sessionId, int turnNumber, int iterationNumber)
    {
        try
        {
            var db = _redis.GetDatabase();
            var turnId = turnNumber.ToString("D3");
            var iterationId = iterationNumber.ToString("D5");

            // Новый формат итераций: данные стадий сохраняются как отдельные ключи
            // mcts:{sessionId}:{turnId}:growth_{iterationId}:{stageName}
            // Необходимо получить каждую стадию отдельно
            var iterationDetails = new IterationDetails
            {
                IterationNumber = iterationNumber,
                TurnNumber = turnNumber
            };

            // Базовый префикс для всех ключей стадий
            var growthBaseKey = $"{RedisKeyPrefix}{sessionId}:{turnId}:growth_{iterationId}";

            _logger.LogInformation($"Searching for iteration data at key prefix: {growthBaseKey}");

            // Получаем данные стадии отбора (selection)
            var selectionData = await db.StringGetAsync($"{growthBaseKey}:selection");
            if (!selectionData.IsNull)
            {
                _logger.LogDebug($"Raw selection data: {selectionData}");

                try
                {
                    var selectionStage = JsonConvert.DeserializeObject<SelectionStageData>(selectionData);

                    // Важно: генерируем TreeNode объекты из строковых данных
                    selectionStage.GenerateTreeNodesFromStateString();

                    iterationDetails.Selection = selectionStage;
                    _logger.LogInformation($"Successfully loaded selection stage data for iteration {iterationNumber}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing selection data");
                    // Создаем минимальные данные для отображения
                    iterationDetails.Selection = new SelectionStageData
                    {
                        SelectedNode = new TreeNode { State = "Error parsing selection data" }
                    };
                }
            }
            else
            {
                _logger.LogWarning($"Selection data not found at {growthBaseKey}:selection");
            }

            // Получаем данные стадии расширения (expansion)
            var expansionData = await db.StringGetAsync($"{growthBaseKey}:expansion");
            if (!expansionData.IsNull)
            {
                _logger.LogDebug($"Raw expansion data: {expansionData}");

                try
                {
                    var expansionStage = JsonConvert.DeserializeObject<ExpansionStageData>(expansionData);

                    // Генерируем TreeNode объекты из строковых данных
                    expansionStage.GenerateTreeNodesFromStateStrings();

                    iterationDetails.Expansion = expansionStage;
                    _logger.LogInformation($"Successfully loaded expansion stage data for iteration {iterationNumber}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing expansion data");
                    // Создаем минимальные данные для отображения
                    iterationDetails.Expansion = new ExpansionStageData
                    {
                        ExpandedNode = new TreeNode { State = "Error parsing expansion data" },
                        NodeForPlayout = new TreeNode { State = "Error parsing expansion data" }
                    };
                }
            }
            else
            {
                _logger.LogWarning($"Expansion data not found at {growthBaseKey}:expansion");
            }

            // Получаем данные стадии моделирования (playout)
            var playoutData = await db.StringGetAsync($"{growthBaseKey}:playout");
            if (!playoutData.IsNull)
            {
                _logger.LogDebug($"Raw playout data: {playoutData}");

                try
                {
                    var playoutStage = JsonConvert.DeserializeObject<PlayoutStageData>(playoutData);

                    // Генерируем TreeNode объекты из строковых данных
                    playoutStage.GenerateTreeNodesFromStateStrings();

                    iterationDetails.Playout = playoutStage;
                    _logger.LogInformation($"Successfully loaded playout stage data for iteration {iterationNumber}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing playout data");
                    // Создаем минимальные данные для отображения
                    iterationDetails.Playout = new PlayoutStageData
                    {
                        StartNode = new TreeNode { State = "Error parsing playout data" },
                        Results = new Dictionary<string, double>()
                    };
                }
            }
            else
            {
                _logger.LogWarning($"Playout data not found at {growthBaseKey}:playout");
            }

            // Получаем данные стадии обратного распространения (backpropagation)
            var backpropagationData = await db.StringGetAsync($"{growthBaseKey}:backpropagation");
            if (!backpropagationData.IsNull)
            {
                _logger.LogDebug($"Raw backpropagation data: {backpropagationData}");

                try
                {
                    var backpropagationStage =
                        JsonConvert.DeserializeObject<BackpropagationStageData>(backpropagationData);

                    // Генерируем TreeNode объекты из строковых данных
                    backpropagationStage.GenerateTreeNodesFromStateStrings();

                    iterationDetails.Backpropagation = backpropagationStage;
                    _logger.LogInformation(
                        $"Successfully loaded backpropagation stage data for iteration {iterationNumber}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing backpropagation data");
                    // Создаем минимальные данные для отображения
                    iterationDetails.Backpropagation = new BackpropagationStageData
                    {
                        Path = new List<TreeNode> { new TreeNode { State = "Error parsing backpropagation data" } },
                        Results = new Dictionary<string, double>()
                    };
                }
            }
            else
            {
                _logger.LogWarning($"Backpropagation data not found at {growthBaseKey}:backpropagation");
            }

            // Проверяем, что хотя бы одна стадия была загружена
            if (iterationDetails.Selection == null &&
                iterationDetails.Expansion == null &&
                iterationDetails.Playout == null &&
                iterationDetails.Backpropagation == null)
            {
                // Проверим, существует ли ключ для итерации в старом формате
                var oldFormatKey = $"{RedisKeyPrefix}{sessionId}:{turnId}:iteration:{iterationId}";
                var oldFormatData = await db.StringGetAsync(oldFormatKey);

                if (!oldFormatData.IsNull)
                {
                    // Если есть данные в старом формате, используем их
                    _logger.LogInformation(
                        $"Using data in old format for iteration {iterationNumber} at key {oldFormatKey}");
                    return JsonConvert.DeserializeObject<IterationDetails>(oldFormatData);
                }

                _logger.LogError($"No data found for any stage of iteration {iterationNumber}");
                throw new InvalidOperationException(
                    $"Iteration data not found for session {sessionId}, turn {turnNumber}, iteration {iterationNumber}");
            }

            return iterationDetails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Error retrieving iteration details for session {sessionId}, turn {turnNumber}, iteration {iterationNumber}");
            throw;
        }
    }

    private int ExtractGrowthNumber(string key)
    {
        var match = Regex.Match(key, @"growth_(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : -1;
    }
}