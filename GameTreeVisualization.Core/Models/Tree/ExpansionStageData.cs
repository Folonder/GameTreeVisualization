using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameTreeVisualization.Core.Models.Tree
{
    public class ExpansionStageData
    {
        // Существующие поля
        [JsonPropertyName("expandedNode")]
        public TreeNode ExpandedNode { get; set; }
        
        [JsonPropertyName("newNodes")]
        public List<TreeNode> NewNodes { get; set; } = new List<TreeNode>();
        
        [JsonPropertyName("nodeForPlayout")]
        public TreeNode NodeForPlayout { get; set; }
        
        // Новые поля для совместимости с фактическим Java-форматом
        [JsonPropertyName("iterationNumber")]
        public int IterationNumber { get; set; }
        
        [JsonPropertyName("expandedNodesCount")]
        public int ExpandedNodesCount { get; set; }
        
        [JsonPropertyName("parentNodeState")]
        public string ParentNodeState { get; set; }
        
        [JsonPropertyName("selectedNodeState")]
        public string SelectedNodeState { get; set; }
        
        // Метод для создания TreeNode объектов из строковых данных
        public void GenerateTreeNodesFromStateStrings()
        {
            // Создаем узел расширения из parentNodeState
            if (!string.IsNullOrEmpty(ParentNodeState))
            {
                ExpandedNode = new TreeNode
                {
                    State = ParentNodeState,
                    Statistics = new NodeStatistics { NumVisits = 1 }
                };
            }
            
            // Создаем новые узлы на основе expandedNodesCount
            NewNodes = new List<TreeNode>();
            if (ExpandedNodesCount > 0)
            {
                // Создаем заглушки для новых узлов
                for (int i = 0; i < ExpandedNodesCount; i++)
                {
                    NewNodes.Add(new TreeNode
                    {
                        State = $"Expanded node {i}",
                        Statistics = new NodeStatistics { NumVisits = 1 }
                    });
                }
            }
            
            // Создаем узел для проигрывания из selectedNodeState
            if (!string.IsNullOrEmpty(SelectedNodeState))
            {
                NodeForPlayout = new TreeNode
                {
                    State = SelectedNodeState,
                    Statistics = new NodeStatistics { NumVisits = 1 }
                };
            }
        }
    }
}