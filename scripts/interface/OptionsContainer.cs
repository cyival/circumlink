using System.Collections.Generic;
using Godot;

namespace Circumlink.Interface;

public partial class OptionsContainer : PanelContainer
{
    [Export]
    private OptionButton _resolutionOptionButton;

    [Export]
    private CheckButton _fullscreenCheckButton;

    [Export]
    private HSlider _volumeSlider;

    private SettingsController _settingsController => Game.Instance.SettingsController;

    public override void _Ready()
    {
        var resolutions = _settingsController.Resolutions;

        foreach (var resolution in resolutions)
        {
            _resolutionOptionButton.AddItem($"{resolution.X}x{resolution.Y}");
        }

        var saveSettings = Game.Instance.Save.Settings;
        var cur = new Vector2I(saveSettings.ResolutionX, saveSettings.ResolutionY);
        var index = resolutions.IndexOf(cur);
        if (index < 0)
            index = 0;

        _resolutionOptionButton.Selected = index;
        _fullscreenCheckButton.ButtonPressed = saveSettings.Fullscreen;
        _volumeSlider.Value = saveSettings.Volume;

        _resolutionOptionButton.ItemSelected += OnResolutionSelected;
        _fullscreenCheckButton.Toggled += OnFullscreenToggled;
        _volumeSlider.ValueChanged += OnVolumeChanged;
    }

    private void OnResolutionSelected(long index)
    {
        var resolutions = _settingsController.Resolutions;
        var i = (int)index;
        if (i < 0 || i >= resolutions.Count)
            return;

        var settings = Game.Instance.Save.Settings;
        settings.ResolutionX = resolutions[i].X;
        settings.ResolutionY = resolutions[i].Y;
        _settingsController.ApplySettings();
    }

    private void OnFullscreenToggled(bool fullscreen)
    {
        Game.Instance.Save.Settings.Fullscreen = fullscreen;
        _settingsController.ApplySettings();
    }

    private void OnVolumeChanged(double volume)
    {
        Game.Instance.Save.Settings.Volume = (float)volume;
        _settingsController.ApplySettings();
    }
}
