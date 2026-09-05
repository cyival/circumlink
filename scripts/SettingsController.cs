using System.Collections.Generic;
using Godot;

namespace Circumlink;

public partial class SettingsController : Node
{
    private GameSettings _gameSettings => Game.Instance.Save.Settings;

    public Vector2I CurrentResolution => new(_gameSettings.ResolutionX, _gameSettings.ResolutionY);

    public List<Vector2I> Resolutions { get; private set; } = [
        new Vector2I(1152, 648),
        new Vector2I(1280, 720),
        new Vector2I(1600, 900),
        new Vector2I(1920, 1080),
        new Vector2I(2560, 1440),
    ];

    public void ApplySettings()
    {
        // Validate resolution
        if (!Resolutions.Contains(CurrentResolution))
        {
            _gameSettings.ResolutionX = Resolutions[0].X;
            _gameSettings.ResolutionY = Resolutions[0].Y;
        }

        // Apply resolution
        GetTree().Root.Size = CurrentResolution;

        // Apply fullscreen
        GetTree().Root.Mode = _gameSettings.Fullscreen ? Window.ModeEnum.Fullscreen : Window.ModeEnum.Windowed;
    }
}
