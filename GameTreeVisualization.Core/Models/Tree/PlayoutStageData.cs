using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameTreeVisualization.Core.Models.Tree
{
    public class PlayoutStageData
    {
        // Существующие поля
        [JsonPropertyName("startNode")]
        public TreeNode StartNode { get; set; }
        
        [JsonPropertyName("depth")]
        public int Depth { get; set; }
        
        [JsonPropertyName("results")]
        public Dictionary<string, double> Results { get; set; } = new Dictionary<string, double>();
        
        // Новые поля для совместимости с Java-форматом
        [JsonPropertyName("iterationNumber")]
        public int IterationNumber { get; set; }
        
        [JsonPropertyName("playoutDepth")]
        public int PlayoutDepth { get; set; }
        
        [JsonPropertyName("startNodeState")]
        public string StartNodeState { get; set; }
        
        [JsonPropertyName("finalState")]
        public string FinalState { get; set; }
        
        [JsonPropertyName("roleScores")]
        public Dictionary<string, double> RoleScores { get; set; }
        
        // Метод для создания TreeNode объектов из строковых данных
        public void GenerateTreeNodesFromStateStrings()
        {
            // Настраиваем глубину из любого доступного источника
            if (PlayoutDepth > 0)
            {
                Depth = PlayoutDepth;
            }
            
            // Создаем начальный узел
            if (!string.IsNullOrEmpty(StartNodeState))
            {
                StartNode = new TreeNode
                {
                    State = StartNodeState,
                    Statistics = new NodeStatistics { NumVisits = 1 }
                };
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