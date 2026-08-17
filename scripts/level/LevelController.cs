using Godot;
using Microsoft.Extensions.Logging;
using Circumlink.Events;

namespace Circumlink.Level;

public partial class LevelController : Node
{
    [Export]
    public Node3D BaseNode { get; set; }

    private LevelGenerator _levelGenerator;
    private ILogger<LevelController> _logger = Debug.Log.GetLogger<LevelController>();

    private Node3D _currentLevelNode;
    private CharacterBody3D _player => Game.Instance.Player;

    public override void _Ready()
    {
        if (BaseNode is null)
            throw new System.NullReferenceException("BaseNode is null");

        var levelRegistry = LevelRegistry.Load();

        _logger.LogInformation("Loaded {} levels from registry.", levelRegistry.GetLevels().Count);
        _logger.LogInformation("Level 0: {}", levelRegistry.GetLevels()[0]);

        _levelGenerator = new LevelGenerator(levelRegistry);
        AddChild(_levelGenerator);

        // Generate the level when the game is ready.
        this.SubscribeEvent<GameReadyEvent>((e) =>
        {
            _currentLevelNode = _levelGenerator.GenerateLevel();
            BaseNode.AddChild(_currentLevelNode);
        });

        this.SubscribeEvent<LevelGeneratedEvent>((e) =>
        {
            var spawnPoint = e.LevelNode.GetNodeOrNull<Marker3D>("PlayerSpawn");
            if (spawnPoint is not null)
                _player.Position = spawnPoint.Position;
            _logger.LogInformation("Sync player position to spawn point: {}", spawnPoint?.Position);
        });
    }
}
