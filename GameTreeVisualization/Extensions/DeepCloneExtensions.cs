using System.Text.Json;

namespace GameTreeVisualization.Extensions;

public static class DeepCloneExtensions
{
    public static T DeepClone<T>(this T source)
    {
        var serialized = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<T>(serialized);
    }
}