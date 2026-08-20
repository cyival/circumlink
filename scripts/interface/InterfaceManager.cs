using Godot;
using Microsoft.Extensions.Logging;

namespace Circumlink.Interface;

// TODO: Not sure when to display hud.
public partial class InterfaceManager : Node
{
    private ILogger<InterfaceManager> _logger = Debug.Log.GetLogger<InterfaceManager>();

    public Control CurrentDisplay { get; private set; }

    public HudInterface Hud { get; private set; }

    public override void _Ready()
    {
        LoadHud();
        _logger.LogInformation("InterfaceManager ready.");
    }

    public void Display(Control display)
    {
        CurrentDisplay = display;
        if (CurrentDisplay is not null)
        {
            // FIXME: May have race conditions if Display is called before the lock is acquired.
            // FIXME: Unlink TreeExiting event when current display switches
            CurrentDisplay.TreeExiting += () =>
            {
                lock (CurrentDisplay)
                {
                    _logger.LogDebug("Display {display} is hiding.", CurrentDisplay);
                    CurrentDisplay = null;
                    Hud.Show();
                }
            };

            _logger.LogDebug("Display {display} is showing.", CurrentDisplay);
            AddChild(CurrentDisplay);

            CurrentDisplay.Show();
            Hud.Hide();
        }
        else
        {
            _logger.LogWarning("Display is null.");
        }
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
