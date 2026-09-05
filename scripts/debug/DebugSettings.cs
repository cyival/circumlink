
using System.Text.Json;
using Godot;

namespace Circumlink.Debug;

struct DebugSettings
{
    public string[] EventLogFilters { get; set; }

    public static DebugSettings Load()
    {
        var jsonString = FileAccess.GetFileAsString("res://debug.json");

        var settings = new JsonSerializerOptions() {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };
        return JsonSerializer.Deserialize<DebugSettings>(jsonString, settings);
    }
}
