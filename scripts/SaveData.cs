using System.Text.Json.Serialization;

namespace Circumlink;

public sealed class SaveData
{
    public GameSettings Settings { get; set; }
}

[JsonSerializable(typeof(SaveData))]
public partial class SaveDataJsonContext : JsonSerializerContext
{}
