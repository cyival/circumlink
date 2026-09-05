using Godot;
using System;
using System.Collections.Generic;

namespace Circumlink.Interface;

public partial class OptionsContainer : PanelContainer
{
    [Export]
    private OptionButton _resolutionOptionButton;

    private SettingsController _settingsController => Game.Instance.SettingsController;

    public override void _Ready()
    {
        var resolutions = _settingsController.Resolutions;

        foreach (var resolution in resolutions)
        {
            _resolutionOptionButton.AddItem($"{resolution.X}x{resolution.Y}");
        }

        var cur = new Vector2I(Game.Instance.Save.Settings.ResolutionX, Game.Instance.Save.Settings.ResolutionY);
        var index = resolutions.IndexOf(cur);

        _resolutionOptionButton.Selected = index;
    }
}
