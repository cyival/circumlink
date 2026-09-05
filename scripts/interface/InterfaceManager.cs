using Godot;
using Microsoft.Extensions.Logging;

namespace Circumlink.Interface;

public partial class InterfaceManager : Node
{
    private ILogger<InterfaceManager> _logger = Debug.Log.GetLogger<InterfaceManager>();

    private Control _currentDisplay;
    private System.Action _currentDisplayTreeExiting;

    public Control CurrentDisplay => _currentDisplay;

    public HudInterface Hud { get; private set; }

    public override void _Ready()
    {
        LoadHud();
        _logger.LogInformation("InterfaceManager ready.");
    }

    public void Display(Control display)
    {
        if (display is null)
        {
            _logger.LogWarning("Display is null.");
            return;
        }

        // Remove any previously active display so switching views doesn't
        // leave stale TreeExiting handlers behind.
        if (_currentDisplay is not null && GodotObject.IsInstanceValid(_currentDisplay))
        {
            if (_currentDisplayTreeExiting is not null)
                _currentDisplay.TreeExiting -= _currentDisplayTreeExiting;

            _currentDisplay.Hide();
            _currentDisplay.QueueFree();
        }

        _currentDisplay = display;
        _currentDisplayTreeExiting = () =>
        {
            _logger.LogDebug("Display {display} is hiding.", display);
            _currentDisplay = null;
            _currentDisplayTreeExiting = null;
            Hud.Show();
        };

        display.TreeExiting += _currentDisplayTreeExiting;

        _logger.LogDebug("Display {display} is showing.", display);
        AddChild(display);
        display.Show();
        Hud.Hide();
    }

    public void ShowMessage(string message) => Hud.ShowMessage(message);
    public void HideMessage() => Hud.HideMessage();

    private void LoadHud()
    {
        Hud = ResourceLoader.Load<PackedScene>("res://scenes/interface/hud.tscn").Instantiate<HudInterface>();
        AddChild(Hud);

        if (CurrentDisplay is not null)
        {
            Hud.Show();
        }
    }
}
