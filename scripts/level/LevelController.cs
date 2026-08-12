using Godot;

namespace Circumlink.Level;

public partial class LevelController : Node
{
    [Export]
    public Node3D BaseNode { get; set; }

    private LevelGenerator _levelGenerator;

    public override void _Ready()
    {
        if (BaseNode is null)
            throw new System.NullReferenceException("BaseNode is null");

        _levelGenerator = GetNodeOrNull<LevelGenerator>("LevelGenerator");

        if (_levelGenerator is null)
        {
            _levelGenerator = new LevelGenerator();
            AddChild(_levelGenerator);
        }
    }
}
