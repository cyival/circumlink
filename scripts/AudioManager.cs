using System;
using Circumlink.Debug;
using Godot;
using Microsoft.Extensions.Logging;

namespace Circumlink;

public partial class AudioManager : Node
{
    private ILogger<AudioManager> _logger = Log.GetLogger<AudioManager>();

    [Export]
    public StringName BgmBusName { get; set; } = "BGM";

    [Export]
    public StringName SfxBusName { get; set; } = "SFX_1";

    [Export]
    public StringName SpecialSfxBusName { get; set; } = "SFX_2";

    [Export]
    public ushort MaxCachedSfxPlayers { get; set; } = 10;

    public override void _Ready()
    {
        _logger.LogInformation("AudioManager ready.");
        _logger.LogInformation("Out: {}, Driver: {}, Mix Rate: {}", AudioServer.OutputDevice, AudioServer.GetDriverName(), AudioServer.GetMixRate());
    }

    public void Play(AudioStream stream, AudioType type)
    {
        switch (type)
        {
            case AudioType.Sfx:
                PlaySfx(stream);
                break;
            case AudioType.Bgm:
                PlayBgm(stream);
                break;
            default:
                throw new InvalidOperationException();
        }
    }

    public void PlaySfx(AudioStream stream)
    {
        _logger.LogInformation("Playing Sfx: {}", stream);
    }

    public void PlayBgm(AudioStream stream)
    {
        _logger.LogInformation("Playing Bgm: {}", stream);
    }

    public enum AudioType
    {
        Sfx,
        Bgm,
    }
}
