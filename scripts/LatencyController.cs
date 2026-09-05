using Circumlink.Events;
using Godot;
using Limbo.Console.Sharp;

namespace Circumlink;

public partial class LatencyController : Node
{
    [Export]
    public float MinRecordLatencySecs { get; set; } = 0.2f;

    [Export]
    public float RecordIntervalSecs { get; set; } = 0.02f;

    [Export]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Current simulated/configured latency in seconds.
    /// </summary>
    public float LatencySecs { get; private set; }

    private EventHub _eventHub => EventHub.Instance;

    public override void _Ready()
    {
        RegisterConsoleCommands();
    }

    public void SetLatency(float value)
    {
        var clamped = Mathf.Max(0f, value);
        if (Mathf.IsEqualApprox(clamped, LatencySecs))
            return;

        LatencySecs = clamped;
        _eventHub?.Publish(new LatencyChangedEvent(LatencySecs));
    }

    public bool ShouldRecord() => IsEnabled && LatencySecs >= MinRecordLatencySecs;

    private void RegisterConsoleCommands()
    {
        LimboConsole.RegisterCommand(new Callable(this, MethodName.CmdLatency), "latency", "Get latency");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.CmdLatencySet), "latency set", "Set latency");
    }

    private void CmdLatency()
    {
        LimboConsole.Info($"Latency: {LatencySecs * 1000f:0} ms");
    }

    private void CmdLatencySet(float value)
    {
        SetLatency(value);
    }
}
