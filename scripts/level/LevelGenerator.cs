using Circumlink.Events;
using Godot;

namespace Circumlink.Level;

public partial class LevelGenerator(LevelRegistry registry) : Node
{
    private LevelRegistry _registry = registry;

    public Node3D GenerateLevel()
    {
        var level = ResourceLoader.Load<PackedScene>("res://scenes/_test_room_1.tscn").Instantiate<Node3D>();
        EventHub.Instance.Publish(new LevelGeneratedEvent(new LevelInfo(), level));
        return level;
    }
}
