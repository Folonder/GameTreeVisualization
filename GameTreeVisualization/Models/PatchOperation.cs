using System.Text.Json;

namespace GameTreeVisualization.Models
{
    public class PatchOperation
    {
        public string Op { get; set; }
        public string Path { get; set; }
        public JsonElement Value { get; set; }
    }
}