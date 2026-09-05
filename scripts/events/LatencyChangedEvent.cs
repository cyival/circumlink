namespace Circumlink.Events;

public record LatencyChangedEvent(float LatencySecs) : IEvent;
