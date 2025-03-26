using System.Text.Json;
using System.Text.Json.Serialization;
using GameTreeVisualization.Converters;
using GameTreeVisualization.Models;
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

            var tree = JsonSerializer.Deserialize<TreeNode>(jsonData, options);
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
}