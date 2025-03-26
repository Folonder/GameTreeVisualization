using System.Text.RegularExpressions;
using GameTreeVisualization.Models;
using GameTreeVisualization.Models.Redis;
using GameTreeVisualization.Models.Tree;
using GameTreeVisualization.Services.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StackExchange.Redis;

namespace GameTreeVisualization.Services
{
    public class GameSessionService : IGameSessionService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<GameSessionService> _logger;
        private readonly string _fileStoragePath;
        private const string RedisKeyPrefix = "mcts:";

        public GameSessionService(
            IConnectionMultiplexer redis,
            IConfiguration configuration,
            ILogger<GameSessionService> logger)
        {
            _redis = redis;
            _logger = logger;
            _fileStoragePath = configuration["Storage:MatchesPath"] ?? "matches";
        }

        public List<int> GetAvailableTurns(string sessionId)
        {
            try
            {
                // Получаем все ключи для данной сессии с финальными состояниями
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var turnKeys = server.Keys(pattern: $"{RedisKeyPrefix}{sessionId}:*:final").ToArray();

                // Извлекаем номера ходов из ключей
                var turns = new List<int>();
                foreach (var key in turnKeys)
                {
                    var keyString = key.ToString();
                    var match = Regex.Match(keyString, $@"{RedisKeyPrefix}{sessionId}:(\d+):final");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int turnNumber))
                    {
                        turns.Add(turnNumber);
                    }
                }

                // Если не нашли по финальным состояниям, ищем по любым ключам роста
                if (turns.Count == 0)
                {
                    var growthKeys = server.Keys(pattern: $"{RedisKeyPrefix}{sessionId}:*:growth_*").ToArray();
                    foreach (var key in growthKeys)
                    {
                        var keyString = key.ToString();
                        var match = Regex.Match(keyString, $@"{RedisKeyPrefix}{sessionId}:(\d+):growth_");
                        if (match.Success && int.TryParse(match.Groups[1].Value, out int turnNumber)
                                          && !turns.Contains(turnNumber))
                        {
                            turns.Add(turnNumber);
                        }
                    }
                }

                return turns.OrderBy(t => t).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving available turns for session {sessionId}");
                throw;
            }
        }

        public async Task<List<TreeGrowthStep>> GetTurnGrowth(string sessionId, int turnNumber)
        {
            try
            {
                List<TreeGrowthStep> growthSteps = new List<TreeGrowthStep>();
                var db = _redis.GetDatabase();
                var turnId = turnNumber.ToString("D3");

                // Получаем все ключи роста дерева для данного хода
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var growthKeys = server.Keys(pattern: $"{RedisKeyPrefix}{sessionId}:{turnId}:growth_*").ToArray()
                    .Select(k => k.ToString())
                    .OrderBy(k =>
                    {
                        var match = Regex.Match(k, @"growth_(\d+)$");
                        return match.Success ? int.Parse(match.Groups[1].Value) : int.MaxValue;
                    })
                    .ToList();

                // Обрабатываем каждый ключ роста
                int stepNumber = 0;
                foreach (var key in growthKeys)
                {
                    var redisTree = await db.StringGetAsync(key);
                    if (redisTree.HasValue)
                    {
                        try
                        {
                            var tree = DeserializeTree(redisTree.ToString());

                            // Извлекаем номер роста из ключа
                            var match = Regex.Match(key, @"growth_(\d+)$");
                            int growthNumber = match.Success ? int.Parse(match.Groups[1].Value) : stepNumber;

                            growthSteps.Add(new TreeGrowthStep
                            {
                                StepNumber = stepNumber++,
                                Turn = turnNumber,
                                PatchNumber = growthNumber,
                                Tree = tree
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error processing growth step from key {key}");
                            // Продолжаем с другими ключами
                        }
                    }
                }

                // Добавляем финальное состояние, если есть
                var finalKey = $"{RedisKeyPrefix}{sessionId}:{turnId}:final";
                var finalTree = await db.StringGetAsync(finalKey);
                if (finalTree.HasValue)
                {
                    try
                    {
                        var tree = DeserializeTree(finalTree.ToString());
                        growthSteps.Add(new TreeGrowthStep
                        {
                            StepNumber = stepNumber,
                            Turn = turnNumber,
                            PatchNumber = -1, // Специальное значение для финального состояния
                            Tree = tree
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing final tree from key {finalKey}");
                    }
                }

                return growthSteps;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving growth steps for session {sessionId}, turn {turnNumber}");
                throw;
            }
        }
        
        public bool SessionExists(string sessionId)
        {
            try
            {
                // Check Redis first
                var db = _redis.GetDatabase();

                // Get all keys matching the pattern for this session
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var matchingKeys = server.Keys(pattern: $"{RedisKeyPrefix}{sessionId}*").ToArray();

                if (matchingKeys.Length > 0)
                {
                    _logger.LogInformation($"Found {matchingKeys.Length} Redis keys for session {sessionId}");
                    return true;
                }

                // If not in Redis, check file system
                var sessionPath = Path.Combine(_fileStoragePath, sessionId);
                bool exists = Directory.Exists(sessionPath);

                _logger.LogInformation($"Session directory {sessionPath} exists: {exists}");
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if session {sessionId} exists");
                return false;
            }
        }

        private TreeNode DeserializeTree(string json)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore
                };

                // Сначала пробуем десериализовать в Redis модель
                var redisTree = JsonConvert.DeserializeObject<RedisTree>(json, settings);

                // Если успешно, конвертируем в TreeNode
                if (redisTree?.Root != null)
                {
                    return ConvertRedisTreeToTreeNode(redisTree);
                }

                // Как запасной вариант, пробуем прямую десериализацию
                return JsonConvert.DeserializeObject<TreeNode>(json, settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deserializing tree: {ex.Message}");
                throw;
            }
        }

        private TreeNode ConvertRedisTreeToTreeNode(RedisTree redisTree)
        {
            // Убедимся, что у нас есть корневой узел
            if (redisTree?.Root == null)
                return null;

            // Преобразуем Redis модель в наш TreeNode
            var tree = ConvertRedisNodeToTreeNode(redisTree.Root, 0);

            // Рассчитываем относительные посещения
            RecalculateRelativeVisits(tree);

            return tree;
        }

        private TreeNode ConvertRedisNodeToTreeNode(RedisNode redisNode, int depth)
        {
            if (redisNode == null)
                return null;

            // Создаем TreeNode из RedisNode
            var treeNode = new TreeNode
            {
                Id = Guid.NewGuid().ToString(),
                // Объединяем все состояния в одну строку
                State = redisNode.State != null ? string.Join(", ", redisNode.State) : "",
                Depth = depth,
                Children = new List<TreeNode>(),
                Statistics = new NodeStatistics
                {
                    NumVisits = redisNode.Statistics?.NumVisits ?? 0,
                    RelativeVisits = 0, // Вычислим позже
                    StatisticsForActions = new List<RoleStatistics>()
                }
            };

            // Обрабатываем статистику действий
            if (redisNode.Statistics?.StatisticsForActions?.Map != null &&
                redisNode.Statistics.StatisticsForActions.Roles != null)
            {
                foreach (var roleInfo in redisNode.Statistics.StatisticsForActions.Roles)
                {
                    string roleName = roleInfo.Name?.Value ?? "";

                    if (redisNode.Statistics.StatisticsForActions.Map.TryGetValue(roleName, out var actionMap))
                    {
                        var roleStats = new RoleStatistics
                        {
                            Role = roleName,
                            Actions = new List<ActionStatistics>()
                        };

                        foreach (var actionEntry in actionMap)
                        {
                            roleStats.Actions.Add(new ActionStatistics
                            {
                                Action = actionEntry.Key,
                                AverageActionScore = actionEntry.Value.ActionScore,
                                ActionNumUsed = actionEntry.Value.ActionNumUsed
                            });
                        }

                        treeNode.Statistics.StatisticsForActions.Add(roleStats);
                    }
                }
            }

            // Рекурсивно обрабатываем детей
            if (redisNode.Children != null)
            {
                foreach (var childNode in redisNode.Children)
                {
                    var child = ConvertRedisNodeToTreeNode(childNode, depth + 1);
                    if (child != null)
                    {
                        treeNode.Children.Add(child);
                    }
                }
            }

            return treeNode;
        }

        private void RecalculateRelativeVisits(TreeNode node)
        {
            if (node == null) return;

            // Если у узла есть дети, вычисляем для них относительные значения
            if (node.Children != null && node.Children.Count > 0)
            {
                int totalChildVisits = node.Children.Sum(c => c.Statistics?.NumVisits ?? 0);

                foreach (var child in node.Children)
                {
                    if (child.Statistics != null && totalChildVisits > 0)
                    {
                        child.Statistics.RelativeVisits = (double)(child.Statistics.NumVisits * 100) / totalChildVisits;
                    }

                    // Рекурсивно обрабатываем детей
                    RecalculateRelativeVisits(child);
                }
            }
        }
    }
}