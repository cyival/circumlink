using Godot;

namespace Circumlink.Events;

public record FixedGhostExpiredEvent(Vector3 Position) : IEvent;
