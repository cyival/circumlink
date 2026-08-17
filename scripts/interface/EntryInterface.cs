using Godot;
using System.IO;

namespace Circumlink.Interface;

public partial class EntryInterface : Control
{
    [Export]
    private Control _warning;

    [Export]
    private Control _screenEffect;

    [Export]
    private AnimationPlayer _animationPlayer;

    private bool _waitingForInput = false;

    public override void _Ready()
    {
        _animationPlayer.AnimationFinished += AnimationFinished;

        // If save not exists, then show warning
        if (!File.Exists(Game.Instance.SaveService.GetSavePath()))
        {
            _screenEffect.Visible = false;
            _warning.Visible = true;
            _waitingForInput = true;
        }
        else
        {
            Game.Instance.LoadGame();
            AnimateEnter();
        }
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (_waitingForInput && @event.IsPressed())
        {
            _waitingForInput = false;
            Game.Instance.SaveGame();
            AnimateEnter();
        }
    }

    private void AnimationFinished(StringName animName)
    {
        if (animName == "start")
        {
            // TODO
        }
    }

    private void AnimateEnter()
    {
        _warning.Visible = false;
        _screenEffect.Visible = true;
        _animationPlayer.Play("start");
    }
}
