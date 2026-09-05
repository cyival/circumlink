using System;
using Godot;

namespace Circumlink;

public partial class PlayerLatencyController : Node
{
    private double _recordTimerSecs;

    private LatencyController _latencyController => Game.Instance.LatencyController;

    public override void _PhysicsProcess(double delta)
    {
        if (!_latencyController.IsEnabled) return;

        _recordTimerSecs += delta;

        // Use a while loop for timer to avoid large gaps between records
        // TODO: Check whether should record like this
        while (_recordTimerSecs >= _latencyController.RecordIntervalSecs)
        {
            _recordTimerSecs -= _latencyController.RecordIntervalSecs;

            RecordState();
        }
    }

    private void RecordState()
    {
        throw new NotImplementedException();
    }
}
