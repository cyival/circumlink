using Godot;
using Microsoft.Extensions.Logging;

namespace Circumlink.Level;

public partial class LevelController : Node
{
    [Export]
    public Node3D BaseNode { get; set; }

    private LevelGenerator _levelGenerator;
    private ILogger<LevelController> _logger = Debug.Log.GetLogger<LevelController>();

    public override void _Ready()
    {
        if (BaseNode is null)
            throw new System.NullReferenceException("BaseNode is null");

        var levelRegistry = LevelRegistry.Load();

        _logger.LogInformation("Loaded {} levels from registry.", levelRegistry.GetLevels().Count);
        _logger.LogInformation("Level 0: {}", levelRegistry.GetLevels()[0]);

        _levelGenerator = new LevelGenerator(levelRegistry);
        AddChild(_levelGenerator);

    }
}
