using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GameTreeVisualization.Models;
using GameTreeVisualization.Models.Redis;
using GameTreeVisualization.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using StackExchange.Redis;
using JsonSerializer = System.Text.Json.JsonSerializer;

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

        public async Task<List<int>> GetAvailableTurns(string sessionId)
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

        public async Task<TreeNode> GetTurnInitialTree(string sessionId, int turnNumber)
        {
            try
            {
                // Получаем начальное дерево для указанного хода
                var db = _redis.GetDatabase();
                var turnId = turnNumber.ToString("D3"); // Форматируем как 001, 002, ...

                // Сначала пытаемся найти ключ growth_00000, который представляет начальное состояние
                var initialGrowthKey = $"{RedisKeyPrefix}{sessionId}:{turnId}:growth_00000";
                var redisTree = await db.StringGetAsync(initialGrowthKey);

                // Если не найдено, пробуем искать ключ init
                if (!redisTree.HasValue)
                {
                    var initKey = $"{RedisKeyPrefix}{sessionId}:{turnId}:init";
                    redisTree = await db.StringGetAsync(initKey);
                }

                // Если все еще не найдено, пробуем поискать любой ключ с growth_ с минимальным номером
                if (!redisTree.HasValue)
                {
                    var server = _redis.GetServer(_redis.GetEndPoints().First());
                    var growthKey = server.Keys(pattern: $"{RedisKeyPrefix}{sessionId}:{turnId}:growth_*")
                        .Select(k => k.ToString())
                        .OrderBy(k =>
                        {
                            var match = Regex.Match(k, @"growth_(\d+)$");
                            return match.Success ? int.Parse(match.Groups[1].Value) : int.MaxValue;
                        })
                        .FirstOrDefault();

                    if (growthKey != null)
                    {
                        redisTree = await db.StringGetAsync(growthKey);
                    }
                }

                if (!redisTree.HasValue)
                {
                    throw new KeyNotFoundException($"Initial tree for turn {turnNumber} not found");
                }

                // Используем TreeProcessingService.DeserializeTree для десериализации
                return DeserializeTree(redisTree.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving initial tree for session {sessionId}, turn {turnNumber}");
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

        public async Task<bool> SessionExists(string sessionId)
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

        public async Task<TreeNode> GetInitialTree(string sessionId)
        {
            try
            {
                // Try to get initial tree from Redis with exact key match
                var db = _redis.GetDatabase();
                var redisTree = await db.StringGetAsync($"{RedisKeyPrefix}{sessionId}:initial");

                // If not found with exact match, find all related initial trees
                if (!redisTree.HasValue)
                {
                    _logger.LogInformation(
                        $"Initial tree not found with exact key {RedisKeyPrefix}{sessionId}:initial");

                    // Get all keys that match the session pattern with :initial suffix
                    var server = _redis.GetServer(_redis.GetEndPoints().First());
                    var initialKeys = server.Keys(pattern: $"{RedisKeyPrefix}{sessionId}*:initial").ToArray();

                    if (initialKeys.Length > 0)
                    {
                        _logger.LogInformation(
                            $"Found {initialKeys.Length} potential initial trees for session {sessionId}");

                        // Take the first one available
                        redisTree = await db.StringGetAsync(initialKeys[0]);
                        _logger.LogInformation($"Using initial tree with key {initialKeys[0]}");
                    }
                }

                if (redisTree.HasValue)
                {
                    try
                    {
                        return DeserializeTree(redisTree.ToString());
                    }
                    catch (JsonSerializationException ex)
                    {
                        _logger.LogError(ex, $"Error deserializing tree from Redis");
                        throw;
                    }
                }

                // Try file system
                var initialFilePath = Path.Combine(_fileStoragePath, sessionId, "step_0.json");
                if (File.Exists(initialFilePath))
                {
                    var treeJson = await File.ReadAllTextAsync(initialFilePath);
                    try
                    {
                        var tree = DeserializeTree(treeJson);

                        // Store in Redis for future access
                        await db.StringSetAsync($"{RedisKeyPrefix}{sessionId}:initial", treeJson);

                        _logger.LogInformation($"Found and cached initial tree from file {initialFilePath}");
                        return tree;
                    }
                    catch (JsonSerializationException ex)
                    {
                        _logger.LogError(ex, $"Error deserializing tree from file");
                        throw;
                    }
                }

                // If there are patches but no initial tree, create an empty one
                var patches = await GetPatches(sessionId);
                if (patches.Count > 0)
                {
                    _logger.LogInformation($"Creating empty initial tree with {patches.Count} patches available");
                    return new TreeNode
                    {
                        State = "Initial Empty State",
                        Statistics = new NodeStatistics { NumVisits = 0, RelativeVisits = 0 },
                        Children = new List<TreeNode>(),
                        Depth = 0
                    };
                }

                _logger.LogWarning($"Initial tree for session {sessionId} not found in Redis or file system");
                throw new FileNotFoundException($"Initial tree for session {sessionId} not found");
            }
            catch (Exception ex) when (!(ex is FileNotFoundException))
            {
                _logger.LogError(ex, $"Error retrieving initial tree for session {sessionId}");
                throw;
            }
        }

        public async Task<List<TreePatch>> GetPatches(string sessionId)
        {
            List<TreePatch> patches = new List<TreePatch>();

            try
            {
                // Check Redis for patches
                var db = _redis.GetDatabase();

                // Get all keys matching the pattern for this session
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var patchKeys = server.Keys(pattern: $"{RedisKeyPrefix}{sessionId}*:patch:*").ToArray();

                _logger.LogInformation($"Found {patchKeys.Length} patch keys for session {sessionId}");

                if (patchKeys.Length > 0)
                {
                    // Extract the patch numbers and sort them
                    var orderedPatchKeys = patchKeys
                        .Select(key =>
                        {
                            try
                            {
                                // Extract the patch number from the key
                                var keyString = key.ToString();
                                var parts = keyString.Split(':');
                                int patchNumber = int.Parse(parts[parts.Length - 1]);
                                return new { Key = key, PatchNumber = patchNumber };
                            }
                            catch
                            {
                                return null;
                            }
                        })
                        .Where(item => item != null)
                        .OrderBy(item => item.PatchNumber)
                        .ToList();

                    // Extract turn number from session ID if possible
                    int turn = 0;
                    var turnMatch = Regex.Match(sessionId, @":(\d+):");
                    if (turnMatch.Success)
                    {
                        turn = int.Parse(turnMatch.Groups[1].Value);
                    }

                    // Get all patch data in one batch
                    var patchKeyArray = orderedPatchKeys.Select(k => (RedisKey)k.Key.ToString()).ToArray();
                    var patchDataValues = await db.StringGetAsync(patchKeyArray);

                    // Process each patch
                    patches = ProcessRedisPatches(
                        patchKeyArray.Select(k => k.ToString()).ToArray(),
                        patchDataValues,
                        turn
                    );
                }

                // If not found in Redis or as fallback, try file system
                if (patches.Count == 0)
                {
                    var patchesDirectory = Path.Combine(_fileStoragePath, sessionId, "mcts_logs");
                    if (Directory.Exists(patchesDirectory))
                    {
                        // Get all patch files in order
                        var patchFiles = Directory.GetFiles(patchesDirectory, "*_patch_*.json")
                            .OrderBy(f =>
                            {
                                // Extract turn number and patch number for sorting
                                var fileName = Path.GetFileName(f);
                                var match = Regex.Match(fileName, @"turn(\d+)_patch_(\d+)");
                                if (match.Success)
                                {
                                    int turn = int.Parse(match.Groups[1].Value);
                                    int patch = int.Parse(match.Groups[2].Value);
                                    return (turn * 1000000) + patch;
                                }

                                return 0;
                            })
                            .ToList();

                        _logger.LogInformation($"Found {patchFiles.Count} patch files in directory {patchesDirectory}");

                        foreach (var file in patchFiles)
                        {
                            try
                            {
                                var patchJson = await File.ReadAllTextAsync(file);
                                var jsonDocument = JsonDocument.Parse(patchJson);
                                var operations = new List<PatchOperation>();

                                if (jsonDocument.RootElement.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var element in jsonDocument.RootElement.EnumerateArray())
                                    {
                                        string op = element.GetProperty("op").GetString();
                                        string path = element.GetProperty("path").GetString();

                                        JsonElement value = default;
                                        if (element.TryGetProperty("value", out var valueElement))
                                        {
                                            value = valueElement;
                                        }

                                        operations.Add(new PatchOperation
                                        {
                                            Op = op,
                                            Path = path,
                                            Value = value
                                        });
                                    }
                                }

                                var fileName = Path.GetFileName(file);
                                var match = Regex.Match(fileName, @"turn(\d+)_patch_(\d+)");

                                if (match.Success)
                                {
                                    int turn = int.Parse(match.Groups[1].Value);
                                    int patch = int.Parse(match.Groups[2].Value);

                                    patches.Add(new TreePatch
                                    {
                                        Turn = turn,
                                        PatchNumber = patch,
                                        Operations = operations
                                    });

                                    _logger.LogInformation(
                                        $"Added patch from file {fileName} with {operations.Count} operations");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error processing patch file {file}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving patches for session {sessionId}");
                throw;
            }

            return patches.OrderBy(p => p.PatchNumber).ToList();
        }

        private List<TreePatch> ProcessRedisPatches(string[] patchKeys, RedisValue[] patchData, int turn)
        {
            List<TreePatch> patches = new List<TreePatch>();

            for (int i = 0; i < patchKeys.Length; i++)
            {
                try
                {
                    var patchKey = patchKeys[i];
                    var patchJson = patchData[i].ToString();

                    // Parse patch number from key
                    var keyParts = patchKey.ToString().Split(':');
                    int patchNumber = int.Parse(keyParts[keyParts.Length - 1]);

                    // Parse the JSON Patch format directly
                    // The patch is already in the correct format as an array of operations
                    var jsonDocument = JsonDocument.Parse(patchJson);
                    var operations = new List<PatchOperation>();

                    if (jsonDocument.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var element in jsonDocument.RootElement.EnumerateArray())
                        {
                            // Extract op, path, and value from each operation
                            string op = element.GetProperty("op").GetString();
                            string path = element.GetProperty("path").GetString();

                            JsonElement value = default;
                            if (element.TryGetProperty("value", out var valueElement))
                            {
                                value = valueElement;
                            }

                            operations.Add(new PatchOperation
                            {
                                Op = op,
                                Path = path,
                                Value = value
                            });
                        }
                    }

                    patches.Add(new TreePatch
                    {
                        Turn = turn,
                        PatchNumber = patchNumber,
                        Operations = operations
                    });

                    _logger.LogInformation($"Added patch {patchNumber} with {operations.Count} operations");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error parsing patch data");
                }
            }

            return patches.OrderBy(p => p.PatchNumber).ToList();
        }

        public async Task<List<TreeGrowthStep>> CalculateTreeGrowth(string sessionId)
        {
            try
            {
                _logger.LogInformation($"Calculating tree growth for session {sessionId}");

                var initialTree = await GetInitialTree(sessionId);
                var patches = await GetPatches(sessionId);

                _logger.LogInformation($"Found initial tree and {patches.Count} patches");

                List<TreeGrowthStep> growthSteps = new List<TreeGrowthStep>();

                // Add initial state
                growthSteps.Add(new TreeGrowthStep
                {
                    StepNumber = 0,
                    Turn = 0,
                    PatchNumber = 0,
                    Tree = JsonClone(initialTree)
                });

                // Apply each patch in sequence
                JToken currentTree = JToken.FromObject(initialTree);
                int stepNumber = 1;

                foreach (var patch in patches)
                {
                    try
                    {
                        // Apply each operation in the patch
                        foreach (var op in patch.Operations)
                        {
                            try
                            {
                                string operationType = op.Op.ToLower();
                                string path = op.Path;

                                // Make sure path starts with /
                                if (!path.StartsWith("/"))
                                    path = "/" + path;

                                JToken value = null;
                                if (op.Value.ValueKind != JsonValueKind.Undefined)
                                {
                                    value = JToken.Parse(op.Value.GetRawText());
                                }

                                switch (operationType)
                                {
                                    case "add":
                                        if (value != null)
                                        {
                                            ApplyAdd(currentTree, path, value);
                                        }

                                        break;

                                    case "remove":
                                        ApplyRemove(currentTree, path);
                                        break;

                                    case "replace":
                                        if (value != null)
                                        {
                                            ApplyReplace(currentTree, path, value);
                                        }

                                        break;

                                    default:
                                        _logger.LogWarning($"Unsupported operation type: {operationType}");
                                        break;
                                }
                            }
                            catch (Exception opEx)
                            {
                                _logger.LogError(opEx, $"Error applying operation: {op.Op} to path {op.Path}");
                                // Continue with next operation
                            }
                        }

                        // Convert patched tree back to TreeNode
                        TreeNode patchedTree = currentTree.ToObject<TreeNode>();

                        // Add to growth steps
                        growthSteps.Add(new TreeGrowthStep
                        {
                            StepNumber = stepNumber++,
                            Turn = patch.Turn,
                            PatchNumber = patch.PatchNumber,
                            Tree = patchedTree
                        });

                        _logger.LogInformation(
                            $"Applied patch {patch.PatchNumber} with {patch.Operations.Count} operations");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error applying patch {patch.PatchNumber}");
                        // Continue with the next patch
                    }
                }

                _logger.LogInformation($"Calculated {growthSteps.Count} tree growth steps");
                return growthSteps;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating tree growth for session {sessionId}");
                throw;
            }
        }

        // JSON Patch helper methods
        private void ApplyAdd(JToken target, string path, JToken value)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || path == "/")
                {
                    throw new InvalidOperationException("Cannot add at root level");
                }

                // Get parent path and the last segment (property name or array index)
                string parentPath = GetParentPath(path);
                string lastSegment = GetLastPathSegment(path);

                // Get the parent token
                JToken parent;
                if (parentPath == "/")
                {
                    parent = target;
                }
                else
                {
                    parent = target.SelectToken(parentPath.TrimStart('/'));
                    if (parent == null)
                    {
                        _logger.LogWarning($"Parent path not found: {parentPath}");
                        return;
                    }
                }

                // Handle array insertions
                if (int.TryParse(lastSegment, out int index))
                {
                    if (parent is JArray array)
                    {
                        if (index < 0)
                        {
                            _logger.LogWarning($"Negative array index: {index}");
                            index = 0;
                        }

                        if (index > array.Count)
                        {
                            _logger.LogWarning($"Index {index} out of bounds, defaulting to end of array");
                            index = array.Count;
                        }

                        if (index == array.Count)
                        {
                            array.Add(value);
                        }
                        else
                        {
                            array.Insert(index, value);
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Cannot insert into non-array at {parentPath}");
                    }
                }
                else
                {
                    // Handle object property additions
                    if (parent is JObject obj)
                    {
                        obj[lastSegment] = value;
                    }
                    else
                    {
                        _logger.LogWarning($"Cannot add property to non-object at {parentPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding token at path {path}");
                throw;
            }
        }

        private void ApplyRemove(JToken target, string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || path == "/")
                {
                    throw new InvalidOperationException("Cannot remove root object");
                }

                // Get parent path and the last segment (property name or array index)
                string parentPath = GetParentPath(path);
                string lastSegment = GetLastPathSegment(path);

                // Get the parent token
                JToken parent;
                if (parentPath == "/")
                {
                    parent = target;
                }
                else
                {
                    parent = target.SelectToken(parentPath.TrimStart('/'));
                    if (parent == null)
                    {
                        _logger.LogWarning($"Parent path not found: {parentPath}");
                        return;
                    }
                }

                // Handle array removals
                if (int.TryParse(lastSegment, out int index))
                {
                    if (parent is JArray array)
                    {
                        if (index < 0 || index >= array.Count)
                        {
                            _logger.LogWarning($"Array index out of bounds: {index}");
                            return;
                        }

                        array.RemoveAt(index);
                    }
                    else
                    {
                        _logger.LogWarning($"Cannot remove from non-array at {parentPath}");
                    }
                }
                else
                {
                    // Handle object property removals
                    if (parent is JObject obj)
                    {
                        obj.Remove(lastSegment);
                    }
                    else
                    {
                        _logger.LogWarning($"Cannot remove property from non-object at {parentPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing token at path {path}");
                throw;
            }
        }

        private void ApplyReplace(JToken target, string path, JToken value)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || path == "/")
                {
                    throw new InvalidOperationException("Cannot replace root object directly");
                }

                // Get parent path and the last segment (property name or array index)
                string parentPath = GetParentPath(path);
                string lastSegment = GetLastPathSegment(path);

                // Get the parent token
                JToken parent;
                if (parentPath == "/")
                {
                    parent = target;
                }
                else
                {
                    parent = target.SelectToken(parentPath.TrimStart('/'));
                    if (parent == null)
                    {
                        _logger.LogWarning($"Parent path not found: {parentPath}");
                        return;
                    }
                }

                // Handle array replacements
                if (int.TryParse(lastSegment, out int index))
                {
                    if (parent is JArray array)
                    {
                        if (index < 0 || index >= array.Count)
                        {
                            _logger.LogWarning($"Array index out of bounds: {index}");
                            return;
                        }

                        array[index] = value;
                    }
                    else
                    {
                        _logger.LogWarning($"Cannot replace in non-array at {parentPath}");
                    }
                }
                else
                {
                    // Handle object property replacements
                    if (parent is JObject obj)
                    {
                        obj[lastSegment] = value;
                    }
                    else
                    {
                        _logger.LogWarning($"Cannot replace property in non-object at {parentPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error replacing token at path {path}");
                throw;
            }
        }

        // Helper methods for path manipulation
        private string GetParentPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "/";

            path = path.TrimEnd('/');
            int lastSlashIndex = path.LastIndexOf('/');

            if (lastSlashIndex <= 0)
                return "/";

            return path.Substring(0, lastSlashIndex);
        }

        private string GetLastPathSegment(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "";

            path = path.TrimEnd('/');
            int lastSlashIndex = path.LastIndexOf('/');

            if (lastSlashIndex < 0)
                return path;

            return path.Substring(lastSlashIndex + 1);
        }

        // Helper method to clone an object using JSON serialization
        private T JsonClone<T>(T obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}