using Circumlink.Events;
using Godot;

namespace Circumlink.Interface;

public partial class HudInterface : Control
{
    [Export]
    private Control _messageDisplay;

    [Export]
    private Label _latencyLabel;

    private Tween _messageTween;

    public override void _Ready()
    {
        this.SubscribeEvent<LatencyChangedEvent>(e => UpdateLatencyLabel(e.LatencySecs));
        UpdateLatencyLabel(Game.Instance?.LatencyController?.LatencySecs ?? 0f);
    }

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

    private void UpdateLatencyLabel(float latencySecs)
    {
        if (_latencyLabel is null)
            return;

        var latencyMs = latencySecs * 1000f;
        _latencyLabel.Text = $"{latencyMs:0}ms";

        if (latencyMs < 200f)
            _latencyLabel.SelfModulate = Colors.Chartreuse;
        else if (latencyMs < 700f)
            _latencyLabel.SelfModulate = Colors.Gold;
        else
            _latencyLabel.SelfModulate = Colors.Red;
    }
}
