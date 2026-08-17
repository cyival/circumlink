
using Circumlink.Level;
using Godot;

namespace Circumlink.Events;

public record LevelGeneratedEvent(LevelInfo LevelInfo, Node3D LevelNode) : IEvent;
