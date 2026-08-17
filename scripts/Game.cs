using Godot;
using Circumlink.Debug;
using Circumlink.Events;

namespace Circumlink;

public partial class Game : Node
{
    public static Game Instance { get; private set; }

    public readonly SaveService SaveService = new(ProjectSettings.GlobalizePath("user://"));
    public readonly EventHub EventHub = new();

    [Export]
    public InterfaceManager InterfaceManager;

    public SaveData Save = new();

    public Game()
    {
        if (Instance != null)
            throw new System.InvalidOperationException("Game instance already exists.");

        Instance = this;
    }

    public override void _Ready()
    {
        AddChild(EventHub);

        Log.LogInformation("Game scene ready.");

        Log.LogInformation("Loading entry ui");

        var entryUi = ResourceLoader.Load<PackedScene>("res://scenes/entry.tscn").Instantiate<Control>();
        InterfaceManager.Display(entryUi);
    }

    public void LoadGame() => Save = SaveService.Load();
    public void SaveGame() => SaveService.Save(Save);
}
