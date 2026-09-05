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

    private AudioPlayerPool _sfxPool;
    private AudioStreamPlayer _bgmPlayer;

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

    /// <summary>
    /// Plays a one-shot SFX through the LRU audio player pool.
    /// </summary>
    public void PlaySfx(AudioStream stream)
    {
        if (stream is null)
        {
            _logger.LogWarning("PlaySfx called with a null stream.");
            return;
        }

        _sfxPool ??= new AudioPlayerPool(this, SfxBusName, MaxCachedSfxPlayers);
        _sfxPool.MaxPlayers = MaxCachedSfxPlayers;
        _sfxPool.Play(stream);
    }

    /// <summary>
    /// Plays a BGM stream on the dedicated BGM bus. Only one BGM plays at a time.
    /// </summary>
    public void PlayBgm(AudioStream stream)
    {
        if (stream is null)
        {
            StopBgm();
            return;
        }

        if (_bgmPlayer is null)
        {
            _bgmPlayer = new AudioStreamPlayer
            {
                Bus = BgmBusName
            };
            AddChild(_bgmPlayer);
        }

        if (_bgmPlayer.Playing && _bgmPlayer.Stream == stream)
            return;

        _bgmPlayer.Stream = stream;
        _bgmPlayer.Play();
    }

    public void StopBgm()
    {
        _bgmPlayer?.Stop();
    }

    public void StopAllSfx()
    {
        _sfxPool?.StopAll();
    }

    public enum AudioType
    {
        Sfx,
        Bgm,
    }
}
