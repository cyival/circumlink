using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using Microsoft.Extensions.Logging;
using Circumlink.Debug;

namespace Circumlink;

public sealed class SaveService
{

    private static readonly SaveDataJsonContext Context = SaveDataJsonContext.Default;
    private static readonly ILogger<SaveService> logger = Log.GetLogger<SaveService>();
    private readonly string _path;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaveService"/> class.
    /// </summary>
    /// <param name="directory">The directory to save the file in. Must be globalized first.</param>
    public SaveService(string directory)
    {
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "save.json");
    }

    public SaveData Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                logger.LogInformation("Creating new save.");
                return new();
            }

            return JsonSerializer.Deserialize(File.ReadAllText(_path), Context.SaveData) ?? new();
        }
        catch (JsonException)
        {
            logger.LogError("Failed to parse save json");
            return new();
        }
        catch (IOException)
        {
            logger.LogError("Failed to open save file");
            return new();
        }
    }

    public void Save(SaveData data)
    {
        File.WriteAllText(_path, JsonSerializer.Serialize(data, Context.SaveData));
    }

    public string GetSavePath()
    {
        return _path;
    }
}
