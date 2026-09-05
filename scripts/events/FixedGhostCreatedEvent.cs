using Godot;

namespace Circumlink.Events;

public record FixedGhostCreatedEvent(Vector3 Position, float LifetimeSecs) : IEvent;
