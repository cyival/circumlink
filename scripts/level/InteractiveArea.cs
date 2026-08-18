
using Circumlink.Interface;
using Godot;

namespace Circumlink.Level;

// TODO: Priority
public partial class InteractiveArea : Area3D
{
    [Export]
    public bool IsInteractive = true;

    [Export]
    public bool Once = false;

    [Export]
    public Node3D HighlightObject;

    [Export]
    public bool ShowMessage = true;

    [Export]
    public string CustomMessage = "";

    private bool _isInteracting = false;
    private PhysicsBody3D _player => Game.Instance.Player;
    private InterfaceManager _interfaceManager => Game.Instance.InterfaceManager;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (!IsInteractive || body != _player || _isInteracting) return;
        _isInteracting = true;
        GD.Print("Body entered: " + body.Name);
        if (ShowMessage) _interfaceManager.ShowMessage(GetMessage());
    }

    private void OnBodyExited(Node3D body)
    {
        if (!IsInteractive || body != _player || !_isInteracting) return;
        _isInteracting = false;
        if (ShowMessage) _interfaceManager.HideMessage();
    }

    // TODO: get key from action
    private string GetMessage()
        => string.IsNullOrEmpty(CustomMessage) ? "Press F to interact" : CustomMessage;
}
