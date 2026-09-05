using Circumlink.Events;
using Godot;

namespace Circumlink;

public partial class LatencyController : Node
{
    [Export]
    public float MinRecordLatencySecs { get; set; }

    [Export]
    public float RecordIntervalSecs { get; set; }

    [Export]
    public bool IsEnabled { get; set; }

    private EventHub _eventHub => EventHub.Instance;
}
