using Godot;
using Circumlink.Debug;
using Circumlink.Events;
using Circumlink.Interface;

namespace Circumlink;

public partial class Game : Node
{
    public static Game Instance { get; private set; }

    public readonly SaveService SaveService = new(ProjectSettings.GlobalizePath("user://"));
    public readonly EventHub EventHub = new();

    [Export]
    public InterfaceManager InterfaceManager { get; private set; }

    [Export]
    public PlayerController Player { get; private set;}

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

        // Set window minimum size
        GetTree().Root.MinSize = new Vector2I(1152, 648);

        Log.LogInformation("Game scene ready.");

        Log.LogInformation("Loading entry ui");

        var entryUi = ResourceLoader.Load<PackedScene>("res://scenes/entry.tscn").Instantiate<Control>();
        InterfaceManager.Display(entryUi);

        EventHub.Publish(new GameReadyEvent());

        // TODO: Maybe this should go to somewhere else
        this.SubscribeEvent<GameEnteredEvent>(_ => {
            Player.IsEnabled = true;
        });
    }

    public void LoadGame() => Save = SaveService.Load();
    public void SaveGame() => SaveService.Save(Save);
}
