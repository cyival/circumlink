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

    public override void _Ready()
    {
        _logger.LogInformation("AudioManager ready.");
        _logger.LogInformation("Out: {}, Driver: {}, Mix Rate: {}", AudioServer.OutputDevice, AudioServer.GetDriverName(), AudioServer.GetMixRate());
    }
}
