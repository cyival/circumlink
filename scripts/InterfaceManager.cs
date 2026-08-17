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
            // FIXME: May have race conditions if Display is called before the lock is acquired.
            _currentDisplay.TreeExiting += () => {
                lock (_currentDisplay)
                {
                    _currentDisplay = null;
                }
            };

            AddChild(_currentDisplay);
            _currentDisplay.Show();
        }
        else
        {
            _logger.LogWarning("Display is null.");
        }
    }
}
