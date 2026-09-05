using Godot;

namespace Circumlink.Interface;

public partial class MenuInterface : Control
{
    [Export]
    public Button ContinueButton;

    [Export]
    public Button SettingsButton;

    [Export]
    public Button ExitButton;

    [Export]
    public PanelContainer MenuPanel;

    [Export]
    public ColorRect ScreenEffect;

    [Export]
    public OptionsContainer OptionsContainer;

    private CameraController _cameraController => Game.Instance.CameraController;

    public override void _Ready()
    {
        OptionsContainer.Hide();

        _cameraController.FocusSubCameraOn(Game.Instance.Player);
        _cameraController.UseSubCamera();

        // Maybe need to put this in InterfaceManager
        Game.Instance.Player.IsControlEnabled = false;

        // idk why is this needed, seems related to sequencing
        Game.Instance.InterfaceManager.Hud.Hide();

        ContinueButton.Pressed += () =>
        {
            var tween = CreateTween();
            tween.SetEase(Tween.EaseType.Out);
            tween.SetTrans(Tween.TransitionType.Cubic);

            tween.SetParallel();
            tween.TweenProperty(MenuPanel, "offset_transform_position", new Vector2(MenuPanel.Size.X + 50, 0), 0.5f);
            tween.TweenProperty(ScreenEffect.Material, "shader_parameter/blur_size", Vector2.Zero, 0.6f);
            tween.SetParallel(false);
            tween.TweenCallback(Callable.From(OnContinueTweenCompleted));

            _cameraController.UseSubCamera(false);
            Game.Instance.Player.IsControlEnabled = true;
        };

        SettingsButton.Pressed += () =>
        {
            OptionsContainer.Visible = !OptionsContainer.Visible;
        };

        ExitButton.Pressed += () =>
        {
            GetTree().Quit();
        };
    }

    private void OnContinueTweenCompleted()
    {
        QueueFree();
    }
}
