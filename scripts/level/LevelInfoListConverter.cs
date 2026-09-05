using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Circumlink.Level;

public class LevelInfoListConverter : JsonConverter<List<LevelInfo>>
{
    public override List<LevelInfo> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected an object.");

        var list = new List<LevelInfo>();

        // 读取外层对象的所有属性
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string id = reader.GetString();  // 外层键名（如 "obj"）

                reader.Read(); // 移动到属性值（内层对象）
                if (reader.TokenType != JsonTokenType.StartObject)
                    throw new JsonException("Inner value must be an object.");

                // 反序列化内层对象为 Item（不含 Id）
                var item = JsonSerializer.Deserialize<LevelInfo>(ref reader, options);
                //if (item == null)
                //    throw new JsonException("Failed to deserialize inner object.");

                item.Id = id;   // 设置外层键为 Id
                list.Add(item);

                // 此时 reader 已经停在 EndObject 之后（Deserialize 已消费完整对象）
            }
        }

        return list;
    }

    public override void Write(Utf8JsonWriter writer, List<LevelInfo> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var item in value)
        {
            writer.WritePropertyName(item.Id);
            // 序列化 Item 时排除 Id 字段，或直接序列化并忽略 Id
            var copy = item with { Id = null }; // 根据实际字段构造
            JsonSerializer.Serialize(writer, copy, options);
        }
        writer.WriteEndObject();
    }
}
