using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameTreeVisualization.Converters;
using GameTreeVisualization.Models;
using GameTreeVisualization.Models.Redis;
using GameTreeVisualization.Services.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace GameTreeVisualization.Services;

public class TreeProcessingService : ITreeProcessingService
{
    private readonly ITreeStorageService _storageService;
    private readonly ILogger<TreeProcessingService> _logger;

    public TreeProcessingService(
        ITreeStorageService storageService,
        ILogger<TreeProcessingService> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<TreeNode> ProcessTreeData(string jsonData)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals | JsonNumberHandling.AllowReadingFromString,
                Converters = { new DoubleConverter() }
            };

            var tree = System.Text.Json.JsonSerializer.Deserialize<TreeNode>(jsonData, options);
            ValidateTree(tree);

            // Проверяем и инициализируем Statistics, если нужно
            if (tree.Statistics == null)
            {
                tree.Statistics = new NodeStatistics();
            }

            // Рассчитываем общее количество посещений на каждом уровне
            var visitsByDepth = GetVisitsByDepth(tree);
        
            // Рассчитываем относительные значения для каждого узла
            CalculateRelativeStatistics(tree, visitsByDepth);
        
            await ProcessNode(tree);
        
            // Сохраняем обработанное дерево
            await _storageService.StoreTree(tree);
        
            return tree;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing tree data: {Message}", ex.Message);
            throw;
        }
    }

    private bool ValidatePatchOperation(PatchOperation op)
    {
        if (string.IsNullOrEmpty(op.Op))
            return false;
            
        string opType = op.Op.ToLower();
        if (opType != "add" && opType != "remove" && opType != "replace" &&
            opType != "move" && opType != "copy" && opType != "test")
        {
            return false;
        }
        
        if (string.IsNullOrEmpty(op.Path))
            return false;
            
        // "value" is required for all operations except "remove"
        if (opType != "remove" && op.Value.ValueKind == System.Text.Json.JsonValueKind.Undefined)
            return false;
            
        return true;
    }

    private void ValidateTree(TreeNode tree)
    {
        if (tree == null)
            throw new ArgumentNullException(nameof(tree));

        if (tree.Children == null)
            tree.Children = new List<TreeNode>();

        foreach (var child in tree.Children)
        {
            ValidateTree(child);
        }
    }

    public async Task<TreeNode> GetCurrentTree()
    {
        return await _storageService.GetStoredTree();
    }

    private async Task ProcessNode(TreeNode node, int depth = 0)
    {
        if (node == null) return;

        node.Depth = depth;

        if (node.Children != null)
        {
            foreach (var child in node.Children)
            {
                await ProcessNode(child, depth + 1);
            }
        }
    }

    public async Task<Dictionary<int, int>> CalculateDepthStatistics(TreeNode tree)
    {
        var stats = new Dictionary<int, int>();
        await CalculateDepthStatsRecursive(tree, stats);
        return stats;
    }

    private async Task CalculateDepthStatsRecursive(TreeNode node, Dictionary<int, int> stats)
    {
        if (node == null) return;

        if (!stats.ContainsKey(node.Depth))
            stats[node.Depth] = 0;

        stats[node.Depth] += node.Statistics?.NumVisits ?? 0;

        if (node.Children != null)
        {
            foreach (var child in node.Children)
            {
                await CalculateDepthStatsRecursive(child, stats);
            }
        }
    }

    private Dictionary<int, int> GetVisitsByDepth(TreeNode node, int depth = 0, Dictionary<int, int> stats = null)
    {
        stats ??= new Dictionary<int, int>();
    
        if (!stats.ContainsKey(depth))
            stats[depth] = 0;
    
        if (node.Statistics == null)
        {
            node.Statistics = new NodeStatistics();
        }
    
        stats[depth] += node.Statistics.NumVisits;

        if (node.Children != null)
        {
            foreach (var child in node.Children)
            {
                GetVisitsByDepth(child, depth + 1, stats);
            }
        }
    
        return stats;
    }

    private void CalculateRelativeStatistics(TreeNode node, Dictionary<int, int> totalVisitsByDepth, int depth = 0)
    {
        if (node.Statistics == null)
        {
            node.Statistics = new NodeStatistics();
        }

        var totalVisitsAtDepth = totalVisitsByDepth.GetValueOrDefault(depth, 0);
        var visits = node.Statistics.NumVisits;
    
        // Добавляем относительную статистику
        if (totalVisitsAtDepth > 0)
        {
            // Вычисляем процент от общего числа посещений на этом уровне
            node.Statistics.RelativeVisits = (double)visits / totalVisitsAtDepth * 100;
        }
        else
        {
            // Если по какой-то причине у нас нет данных о общем количестве, устанавливаем 100%
            // для корневого узла или 0% для других узлов без данных
            node.Statistics.RelativeVisits = depth == 0 ? 100.0 : 0.0;
        }

        // Для дочерних узлов также нужно рассчитать относительную долю от родителя
        if (node.Children != null && node.Children.Count > 0)
        {
            // Суммарные посещения всех непосредственных дочерних узлов
            var totalChildrenVisits = node.Children.Sum(c => c.Statistics?.NumVisits ?? 0);
        
            foreach (var child in node.Children)
            {
                // Рекурсивно обрабатываем детей с увеличенной глубиной
                CalculateRelativeStatistics(child, totalVisitsByDepth, depth + 1);
            
                // Дополнительно вычисляем относительные значения для каждого ребенка
                // относительно родителя (для более точной визуализации)
                if (child.Statistics != null && totalChildrenVisits > 0)
                {
                    var childVisits = child.Statistics.NumVisits;
                    // Это значение показывает, какой процент от посещений родителя приходится на этого ребенка
                    child.Statistics.RelativeVisits = (double)childVisits / totalChildrenVisits * 100;
                }
            }
        }
    }
    
    // Новые методы для работы с Redis моделью
    
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