using Godot;

namespace Circumlink.Interface;

public partial class HudInterface : Control
{
    [Export]
    private Control _messageDisplay;

    private Tween _messageTween;

    public void ShowMessage(string message)
    {
        _messageDisplay.Visible = true;
        _messageDisplay.GetChild<Label>(0).Text = message;
        _messageDisplay.Modulate = Colors.Transparent;

        _messageTween?.Kill();
        _messageTween = CreateTween();
        _messageTween.TweenProperty(_messageDisplay, "modulate", Colors.White, 0.1f);
    }

    public void HideMessage()
    {
        _messageTween?.Kill();
        _messageDisplay.Visible = false;
        _messageDisplay.GetChild<Label>(0).Text = "";
    }
}
