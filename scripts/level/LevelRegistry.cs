using System.Text.Json;
using System.Collections.Generic;
using Godot;

namespace Circumlink.Level;

public class LevelRegistry(List<LevelInfo> levels)
{
    const string LevelInfoPath = "res://levels.json";

    private List<LevelInfo> _levels = levels;

    public static LevelRegistry Load()
    {
        Debug.Log.LogInformation("Loading level registry...");
        var json = FileAccess.GetFileAsString(LevelInfoPath);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
        options.Converters.Add(new LevelInfoListConverter());

        var levels = JsonSerializer.Deserialize<List<LevelInfo>>(json, options);

        return new LevelRegistry(levels);
    }

    public void Add(LevelInfo level)
    {
        _levels.Add(level);
    }

    public List<LevelInfo> GetLevels()
    {
        return _levels;
    }
}
