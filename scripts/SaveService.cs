using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using Microsoft.Extensions.Logging;
using Circumlink.Debug;

namespace Circumlink;

public sealed class SaveService
{
    public const int CurrentSaveVersion = 1;

    private const string SaveFileName = "save.json";
    private const string TempSaveFileName = "save.json.tmp";

    private static readonly SaveDataJsonContext Context = SaveDataJsonContext.Default;
    private static readonly ILogger<SaveService> logger = Log.GetLogger<SaveService>();

    private readonly string _path;
    private readonly string _tempPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaveService"/> class.
    /// </summary>
    /// <param name="directory">The directory to save the file in. Must be globalized first.</param>
    public SaveService(string directory)
    {
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, SaveFileName);
        _tempPath = Path.Combine(directory, TempSaveFileName);
    }

    public SaveData Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                logger.LogInformation("Creating new save.");
                return CreateDefaultSave();
            }

            var data = JsonSerializer.Deserialize(File.ReadAllText(_path), Context.SaveData) ?? CreateDefaultSave();
            return Normalize(data);
        }
        catch (JsonException)
        {
            logger.LogError("Failed to parse save json");
            return CreateDefaultSave();
        }
        catch (IOException)
        {
            logger.LogError("Failed to open save file");
            return CreateDefaultSave();
        }
    }

    public void Save(SaveData data)
    {
        var json = JsonSerializer.Serialize(Normalize(data), Context.SaveData);

        // Write to a temporary file first, then atomically replace the real save.
        // This prevents a crash mid-write from corrupting save.json.
        File.WriteAllText(_tempPath, json);
        File.Move(_tempPath, _path, overwrite: true);
    }

    public string GetSavePath()
    {
        return _path;
    }

    private static SaveData CreateDefaultSave()
    {
        return new SaveData
        {
            SaveVersion = CurrentSaveVersion,
            Settings = new GameSettings()
        };
    }

    private static SaveData Normalize(SaveData data)
    {
        data.Settings ??= new GameSettings();

        if (data.SaveVersion != CurrentSaveVersion)
            logger.LogInformation("Migrating save from version {OldVersion} to {NewVersion}.", data.SaveVersion, CurrentSaveVersion);

        data.SaveVersion = CurrentSaveVersion;
        return data;
    }
}
