using Godot;
using Microsoft.Extensions.Logging;

namespace Circumlink.Interface;

// TODO: Not sure when to display hud.
public partial class InterfaceManager : Node
{
    private ILogger<InterfaceManager> _logger = Debug.Log.GetLogger<InterfaceManager>();

    private Control _currentDisplay;

    private HudInterface _hud;

    public override void _Ready()
    {
        LoadHud();
        _logger.LogInformation("InterfaceManager ready.");
    }

    public void Display(Control display)
    {
        _currentDisplay = display;
        if (_currentDisplay is not null)
        {
            // FIXME: May have race conditions if Display is called before the lock is acquired.
            _currentDisplay.TreeExiting += () =>
            {
                lock (_currentDisplay)
                {
                    _logger.LogDebug("Display {display} is hiding.", _currentDisplay);
                    _currentDisplay = null;
                    _hud.Show();
                }
            };

            AddChild(_currentDisplay);
            _currentDisplay.Show();
            _hud.Hide();
            _logger.LogDebug("Display {display} is showing.", _currentDisplay);
        }
        else
        {
            _logger.LogWarning("Display is null.");
        }
    }

    public void ShowMessage(string message) => _hud.ShowMessage(message);
    public void HideMessage() => _hud.HideMessage();

    private void LoadHud()
    {
        _hud = ResourceLoader.Load<PackedScene>("res://scenes/interface/hud.tscn").Instantiate<HudInterface>();
        AddChild(_hud);

        if (_currentDisplay is not null)
        {
            _hud.Show();
        }
    }
}
