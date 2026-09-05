using Godot;

namespace Circumlink;

public sealed class GameSettings
{
    public int ResolutionX { get; set; } = 1152;
    public int ResolutionY { get; set; } = 648;

    public bool Fullscreen { get; set; } = false;
}
