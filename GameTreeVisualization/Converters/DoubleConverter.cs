using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameTreeVisualization.Converters;

public class DoubleConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (double.TryParse(stringValue, 
                    NumberStyles.Any, 
                    CultureInfo.InvariantCulture, 
                    out double result))
            {
                return result;
            }
            // Обработка специальных значений
            return stringValue?.ToLower() switch
            {
                "infinity" or "+infinity" => double.PositiveInfinity,
                "-infinity" => double.NegativeInfinity,
                "nan" => double.NaN,
                _ => 0
            };
        }
        return reader.GetDouble();
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        if (double.IsNaN(value))
        {
            writer.WriteStringValue("NaN");
        }
        else if (double.IsPositiveInfinity(value))
        {
            writer.WriteStringValue("Infinity");
        }
        else if (double.IsNegativeInfinity(value))
        {
            writer.WriteStringValue("-Infinity");
        }
        else
        {
            writer.WriteNumberValue(value);
        }
    }
}