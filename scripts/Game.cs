using Godot;
using Circumlink.Debug;
using Circumlink.Events;
using Circumlink.Interface;

namespace Circumlink;

public partial class Game : Node
{
    public static Game Instance { get {
        if (field is null)
            Log.LogError("Game instance reached before initialized.");
        return field;
    } private set; }

    public readonly SaveService SaveService = new(ProjectSettings.GlobalizePath("user://"));
    public readonly EventHub EventHub = new();
    public readonly SettingsController SettingsController = new();

    [Export]
    public InterfaceManager InterfaceManager { get; private set; }

    [Export]
    public CameraController CameraController { get; private set; }

    [Export]
    public LatencyController LatencyController { get; private set; }

    [Export]
    public AudioManager AudioManager { get; private set; }

    [Export]
    public PlayerController Player { get; private set; }

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
        AddChild(SettingsController);

        // Set window minimum size
        GetTree().Root.MinSize = new Vector2I(1152, 648);

        Log.LogInformation("Game scene ready.");

        Log.LogInformation("Loading entry ui");

        var entryUi = ResourceLoader.Load<PackedScene>("res://scenes/entry.tscn").Instantiate<Control>();
        InterfaceManager.Display(entryUi);

        LoadDebugSettings();

        EventHub.Publish(new GameReadyEvent());

        // TODO: Maybe this should go to somewhere else
        this.SubscribeEvent<GameEnteredEvent>(_ => {
            Player.IsEnabled = true;
            var menu = ResourceLoader.Load<PackedScene>("res://scenes/interface/menu.tscn").Instantiate<Control>();
            InterfaceManager.Display(menu);
        });
    }

    public void LoadGame()
    {
        Save = SaveService.Load();
        SettingsController.ApplySettings();
    }

    public void SaveGame()
    {
        Save.Settings ??= new GameSettings();
        SaveService.Save(Save);
    }

    private void LoadDebugSettings()
    {
        var debugSettings = DebugSettings.Load();

        EventHub.EventLogFilter = debugSettings.EventLogFilters;
        Log.LogDebug("Event log filter: {Filter}", string.Join(", ", debugSettings.EventLogFilters));
    }
}
