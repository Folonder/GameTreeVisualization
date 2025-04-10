using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameTreeVisualization.Core.Models.Tree
{
    public class BackpropagationStageData
    {
        // Существующие поля
        [JsonPropertyName("path")]
        public List<TreeNode> Path { get; set; } = new List<TreeNode>();
        
        [JsonPropertyName("results")]
        public Dictionary<string, double> Results { get; set; } = new Dictionary<string, double>();
        
        // Новые поля для совместимости с Java-форматом
        [JsonPropertyName("iterationNumber")]
        public int IterationNumber { get; set; }
        
        [JsonPropertyName("pathLength")]
        public int PathLength { get; set; }
        
        [JsonPropertyName("pathNodes")]
        public List<string> PathNodes { get; set; }
        
        [JsonPropertyName("roleScores")]
        public Dictionary<string, double> RoleScores { get; set; }
        
        // Метод для создания TreeNode объектов из строковых данных
        public void GenerateTreeNodesFromStateStrings()
        {
            // Создаем путь из строковых состояний, если есть
            Path = new List<TreeNode>();
            if (PathNodes != null && PathNodes.Count > 0)
            {
                foreach (var state in PathNodes)
                {
                    Path.Add(new TreeNode
                    {
                        State = state,
                        Statistics = new NodeStatistics { NumVisits = 1 }
                    });
                }
            }
            else if (PathLength > 0)
            {
                // Если есть только длина пути, создаем заглушки
                for (int i = 0; i < PathLength; i++)
                {
                    Path.Add(new TreeNode
                    {
                        State = $"Path node {i}",
                        Statistics = new NodeStatistics { NumVisits = 1 }
                    });
                }
            }
            
            // Настраиваем результаты из любого доступного источника
            if (RoleScores != null && RoleScores.Count > 0)
            {
                Results = new Dictionary<string, double>(RoleScores);
            }
            else if (Results == null)
            {
                Results = new Dictionary<string, double>();
            }
        }
    }
}