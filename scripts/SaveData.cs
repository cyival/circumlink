using System.Text.Json.Serialization;

namespace Circumlink;

public sealed class SaveData
{
    public int SaveVersion { get; set; } = 1;

    public GameSettings Settings { get; set; } = new();
}

[JsonSerializable(typeof(SaveData))]
public partial class SaveDataJsonContext : JsonSerializerContext
{}
