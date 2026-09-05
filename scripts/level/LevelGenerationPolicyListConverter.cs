using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Circumlink.Level;

public class LevelGenerationPolicyListConverter : JsonConverter<List<LevelGenerationPolicy>>
{
    public override List<LevelGenerationPolicy> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString()!;
            return new List<LevelGenerationPolicy> { LevelGenerationPolicy.Parse(str) };
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var list = new List<LevelGenerationPolicy>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;
                if (reader.TokenType == JsonTokenType.String)
                {
                    var str = reader.GetString()!;
                    list.Add(LevelGenerationPolicy.Parse(str));
                }
                else
                {
                    throw new JsonException("Array elements must be strings.");
                }
            }
            return list;
        }

        throw new JsonException("Expected a string or an array of strings.");
    }

    public override void Write(Utf8JsonWriter writer, List<LevelGenerationPolicy> value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        if (value.Count == 1)
        {
            writer.WriteStringValue(value[0].ToString());
        }
        else
        {
            writer.WriteStartArray();
            foreach (var item in value)
            {
                writer.WriteStringValue(item.ToString());
            }
            writer.WriteEndArray();
        }
    }
}
