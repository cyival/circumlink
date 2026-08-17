using Godot;
using Microsoft.Extensions.Logging;

namespace Circumlink;

public partial class InterfaceManager : Node
{
    private ILogger<InterfaceManager> _logger = Debug.Log.GetLogger<InterfaceManager>();

    private Control _currentDisplay;

    public override void _Ready()
    {
        _logger.LogInformation("InterfaceManager ready.");
    }

    public void Display(Control display)
    {
        _currentDisplay = display;
        if (_currentDisplay != null)
        {
            AddChild(_currentDisplay);
            _currentDisplay.Show();
        }
        else
        {
            _logger.LogWarning("Display is null.");
        }
    }
}
