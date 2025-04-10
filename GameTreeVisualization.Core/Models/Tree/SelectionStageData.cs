using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameTreeVisualization.Core.Models.Tree
{
    // Промежуточная модель, соответствующая данным, приходящим от Java
    public class JavaSelectionStageData
    {
        [JsonPropertyName("iterationNumber")]
        public int IterationNumber { get; set; }
        
        [JsonPropertyName("pathLength")]
        public int PathLength { get; set; }
        
        [JsonPropertyName("selectedNodeState")]
        public string SelectedNodeState { get; set; }
        
        [JsonPropertyName("isTerminal")]
        public bool IsTerminal { get; set; }
        
        [JsonPropertyName("isLeaf")]
        public bool IsLeaf { get; set; }
        
        // Другие возможные поля...
    }
    
    // Расширение существующей модели для поддержки альтернативного формата
    public class SelectionStageData
    {
        [JsonPropertyName("path")]
        public List<TreeNode> Path { get; set; } = new List<TreeNode>();
        
        [JsonPropertyName("selectedNode")]
        public TreeNode SelectedNode { get; set; }
        
        // Новые свойства, соответствующие Java-формату
        [JsonPropertyName("iterationNumber")]
        public int IterationNumber { get; set; }
        
        [JsonPropertyName("pathLength")]
        public int PathLength { get; set; }
        
        [JsonPropertyName("selectedNodeState")]
        public string SelectedNodeState { get; set; }
        
        [JsonPropertyName("isTerminal")]
        public bool IsTerminal { get; set; }
        
        [JsonPropertyName("isLeaf")]
        public bool IsLeaf { get; set; }
        
        // Метод для создания синтетического пути и узла из строковых данных
        public void GenerateTreeNodesFromStateString()
        {
            if (string.IsNullOrEmpty(SelectedNodeState))
                return;
                
            // Создаем синтетический выбранный узел
            SelectedNode = new TreeNode
            {
                State = SelectedNodeState,
                Statistics = new NodeStatistics { NumVisits = 1 }
            };
            
            // Создаем синтетический путь
            Path = new List<TreeNode>();
            if (PathLength > 0)
            {
                // Добавляем узлы пути с упрощенными состояниями
                for (int i = 0; i < PathLength; i++)
                {
                    Path.Add(new TreeNode
                    {
                        State = i == PathLength - 1 ? SelectedNodeState : $"Path node {i}",
                        Statistics = new NodeStatistics { NumVisits = 1 }
                    });
                }
            }
        }
    }
}