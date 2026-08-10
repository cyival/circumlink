using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Circumlink.Level;

public class LevelInfoListConverter : JsonConverter<List<LevelInfo>>
{
    public override List<LevelInfo> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, List<LevelInfo> value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
