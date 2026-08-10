using Godot;
using Circumlink.Debug;

namespace Circumlink;

public partial class Game : Node
{
    public static Game Instance { get; private set; }

    private readonly SaveService _saveService = new(ProjectSettings.GlobalizePath("user://"));

    public Game()
    {
        if (Instance != null)
            throw new System.InvalidOperationException("Game instance already exists.");

        Instance = this;
    }

    public override void _Ready()
    {
        Log.LogInformation("Game scene ready.");
    }
}
