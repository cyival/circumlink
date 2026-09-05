using System;
using System.Collections.Generic;
using Circumlink.Events;
using Godot;
using Limbo.Console.Sharp;

namespace Circumlink;

public partial class PlayerLatencyController : Node3D
{
    [Export]
    public CharacterBody3D Player { get; set; }

    [Export]
    public int MaxGhosts { get; set; } = 10;

    [Export]
    public float FadeDuration { get; set; } = 0.8f;   // Kept for parity; unused in GDS as well.

    [Export]
    public int HistoryLength { get; set; } = 200;

    [Export]
    public float StepBetweenGhosts { get; set; } = 0.05f;

    [Export]
    public int VisibleGhosts { get; set; } = 1;

    [Export]
    public bool SyncStepsWithLatency { get; set; } = true;

    [Export]
    public float FixedGhostLifetime { get; set; } = 3.0f;

    [Export]
    public int MaxFixedGhosts { get; set; } = 10;

    [Export]
    public float GhostStartAlpha { get; set; } = 0.7f;

    [Export]
    public bool DebugDrawPositions { get; set; } = false;

    [Export]
    public int DebugDrawMax { get; set; } = 30;

    [Export]
    public float DebugMarkerSize { get; set; } = 0.05f;

    [Export]
    public bool DebugShowIndex { get; set; } = false;

    [Export]
    public bool DebugShowTime { get; set; } = false;

    private LatencyController _latencyController => Game.Instance?.LatencyController;

    private readonly struct GhostState
    {
        public readonly Vector3 Position;
        public readonly Vector3 Scale;
        public readonly Texture2D Texture;
        public readonly string AnimationName;
        public readonly int Frame;
        public readonly double Timestamp;

        public GhostState(Vector3 position, Vector3 scale, Texture2D texture, string animationName, int frame, double timestamp)
        {
            Position = position;
            Scale = scale;
            Texture = texture;
            AnimationName = animationName;
            Frame = frame;
            Timestamp = timestamp;
        }
    }

    private struct GhostPose
    {
        public Vector3 Position;
        public Vector3 Scale;
        public Texture2D Texture;
        public bool Valid;
    }

    // Circular history buffer. Timestamps are strictly increasing from oldest to newest.
    private GhostState[] _history = [];
    private int _historyHead;
    private int _historyCount;

    // Dynamic ghost pool (the "afterimages").
    private StaticBody3D[] _ghostBodies = [];
    private Sprite3D[] _ghostSprites = [];

    // Fixed ghost pool (phase 3).
    private StaticBody3D[] _fixedGhostBodies = [];
    private Sprite3D[] _fixedGhostSprites = [];
    private CollisionShape3D[] _fixedGhostShapes = [];
    private bool[] _fixedGhostPendingEnable = [];
    private double[] _fixedGhostRemaining = [];

    // Debug draw (phase 5).
    private MeshInstance3D[] _debugMarkers = [];
    private Label3D[] _debugLabels = [];
    private StandardMaterial3D _debugMaterial;

    private double _sampleTimer;
    private double _sampleInterval = 0.02;

    // Cached player child nodes to avoid scanning the player's children every sample.
    private Node _visualChild;
    private CollisionShape3D _playerCollisionShape;

    public override void _Ready()
    {
        var latencyController = _latencyController;
        _sampleInterval = latencyController is null
            ? 0.02
            : Math.Max(latencyController.RecordIntervalSecs, 0.0001);

        _history = new GhostState[Math.Max(1, HistoryLength)];

        CachePlayerParts();
        SetupGhostPool();
        SetupFixedGhostPool();
        RegisterConsoleCommands();

        if (DebugDrawPositions)
        {
            SetupDebugMarkers();
            SetupDebugLabels();
        }

        PrefillHistory();
    }

    public override void _Process(double delta)
    {
        var latencyController = _latencyController;
        if (latencyController is null || !latencyController.IsEnabled || Player is null)
            return;

        var latency = latencyController.LatencySecs;
        var displayCount = Math.Min(VisibleGhosts, MaxGhosts);

        if (displayCount <= 0 || latency < latencyController.MinRecordLatencySecs)
        {
            HideAllDynamicSprites();
            return;
        }

        var now = Time.GetTicksMsec() / 1000.0;

        // The history must reach back far enough to feed the oldest ghost.
        var neededTime = latency * displayCount;
        if (_historyCount < 2 || (now - GetHistoryFromOldest(0).Timestamp) < neededTime)
        {
            HideAllDynamicSprites();
            return;
        }

        if (SyncStepsWithLatency)
            StepBetweenGhosts = latency;

        var ghostCount = Math.Min(displayCount, _historyCount);
        var step = StepBetweenGhosts;

        for (var i = 0; i < ghostCount; i++)
        {
            var targetTime = now - latency - i * step;

            if (TryComputeGhostPose(targetTime, out var pose))
            {
                var sprite = _ghostSprites[i];
                sprite.GlobalPosition = pose.Position;
                sprite.Scale = pose.Scale;
                if (pose.Texture is not null)
                    sprite.Texture = pose.Texture;
                sprite.Visible = true;

                var alpha = GhostStartAlpha * (1.0f - (float)i / ghostCount);
                var modulate = sprite.Modulate;
                modulate.A = Mathf.Clamp(alpha, 0.1f, 1.0f);
                sprite.Modulate = modulate;
            }
            else
            {
                _ghostSprites[i].Visible = false;
            }
        }

        // Hide unused dynamic sprites.
        for (var i = ghostCount; i < _ghostSprites.Length; i++)
            _ghostSprites[i].Visible = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        var latencyController = _latencyController;
        if (latencyController is null || !latencyController.IsEnabled || Player is null)
            return;

        var latency = latencyController.LatencySecs;
        var displayCount = Math.Min(VisibleGhosts, MaxGhosts);

        if (displayCount <= 0 || latency < latencyController.MinRecordLatencySecs)
        {
            HideAllGhosts();
            return;
        }

        // Sample player state at a fixed rate (default 50 Hz).
        _sampleInterval = Math.Max(latencyController.RecordIntervalSecs, 0.0001);
        _sampleTimer += delta;

        // Cap the catch-up samples so a huge frame hitch cannot cause a death spiral.
        var maxSamples = Math.Max(1, HistoryLength);
        while (_sampleTimer >= _sampleInterval && maxSamples-- > 0)
        {
            _sampleTimer -= _sampleInterval;
            RecordState();
        }

        if (_sampleTimer >= _sampleInterval)
            _sampleTimer = 0.0;

        // Trim old history from the front. With the circular buffer this is O(1).
        var historyMax = Math.Max(1, HistoryLength);
        while (_historyCount > historyMax)
        {
            _historyHead = (_historyHead + 1) % _history.Length;
            _historyCount--;
        }

        var now = Time.GetTicksMsec() / 1000.0;
        var ghostCount = Math.Min(displayCount, _historyCount);
        var step = SyncStepsWithLatency ? latency : StepBetweenGhosts;

        // Update dynamic ghost bodies. Sprites are updated in _Process.
        for (var i = 0; i < ghostCount; i++)
        {
            var targetTime = now - latency - i * step;

            if (TryComputeGhostPose(targetTime, out var pose))
            {
                var body = _ghostBodies[i];
                body.GlobalPosition = pose.Position;
                body.Scale = pose.Scale;
                body.Visible = true;
            }
            else
            {
                _ghostBodies[i].Visible = false;
            }
        }

        for (var i = ghostCount; i < _ghostBodies.Length; i++)
            _ghostBodies[i].Visible = false;

        UpdateFixedGhosts(delta);
        CheckPendingFixedGhosts();

        if (DebugDrawPositions)
            UpdateDebugMarkers();
    }

    private void RecordState()
    {
        if (Player is null)
            return;

        Texture2D texture = null;
        string animationName = "";
        var frame = 0;

        if (_visualChild is Sprite3D sprite)
        {
            texture = sprite.Texture;
        }
        else if (_visualChild is AnimatedSprite3D animatedSprite)
        {
            animationName = animatedSprite.Animation;
            frame = animatedSprite.Frame;
        }

        HistoryAdd(new GhostState(
            Player.GlobalPosition,
            Player.Scale,
            texture,
            animationName,
            frame,
            Time.GetTicksMsec() / 1000.0
        ));
    }

    private void PrefillHistory()
    {
        if (Player is null)
            return;

        var now = Time.GetTicksMsec() / 1000.0;
        var latencyController = _latencyController;
        var latency = latencyController?.LatencySecs ?? 0f;
        var requiredTime = latency * Math.Max(0, VisibleGhosts) + 0.5;
        var steps = (int)(requiredTime / _sampleInterval);
        var texture = GetPlayerTexture();

        for (var i = steps; i >= 0; i--)
        {
            var pastTime = now - i * _sampleInterval;
            HistoryAdd(new GhostState(
                Player.GlobalPosition,
                Player.Scale,
                texture,
                "",
                0,
                pastTime
            ));
        }
    }

    /// <summary>
    /// Interpolates (position + scale) between the two nearest history entries.
    /// Texture is picked from the closest side, matching the GDS behavior.
    /// Zero allocations; called from both _Process and _PhysicsProcess.
    /// </summary>
    private bool TryComputeGhostPose(double targetTime, out GhostPose pose)
    {
        pose = default;

        if (_historyCount == 0)
            return false;

        if (_historyCount == 1)
        {
            var only = GetHistoryFromOldest(0);
            pose.Position = only.Position;
            pose.Scale = only.Scale;
            pose.Texture = only.Texture;
            pose.Valid = true;
            return true;
        }

        var lo = 0;
        var hi = _historyCount - 1;
        while (lo < hi)
        {
            var mid = (lo + hi) / 2;
            if (GetHistoryFromOldest(mid).Timestamp < targetTime)
                lo = mid + 1;
            else
                hi = mid;
        }

        var idx0 = Math.Max(lo - 1, 0);
        var idx1 = Math.Min(lo, _historyCount - 1);
        var state0 = GetHistoryFromOldest(idx0);
        var state1 = GetHistoryFromOldest(idx1);

        double t = 0.0;
        var timeDiff = state1.Timestamp - state0.Timestamp;
        if (timeDiff > 0.0)
            t = Math.Clamp((targetTime - state0.Timestamp) / timeDiff, 0.0, 1.0);

        var weight = (float)t;
        pose.Position = state0.Position.Lerp(state1.Position, weight);
        pose.Scale = state0.Scale.Lerp(state1.Scale, weight);
        pose.Texture = t > 0.5 ? state1.Texture : state0.Texture;
        pose.Valid = true;
        return true;
    }

    private void CachePlayerParts()
    {
        _visualChild = null;
        _playerCollisionShape = null;

        if (Player is null)
            return;

        foreach (var child in Player.GetChildren())
        {
            if (_visualChild is null && (child is Sprite3D || child is AnimatedSprite3D))
                _visualChild = child;

            if (_playerCollisionShape is null && child is CollisionShape3D collisionShape)
                _playerCollisionShape = collisionShape;
        }
    }

    private Texture2D GetPlayerTexture()
    {
        if (_visualChild is Sprite3D sprite)
            return sprite.Texture;

        if (_visualChild is AnimatedSprite3D animatedSprite && animatedSprite.SpriteFrames is not null)
        {
            var frames = animatedSprite.SpriteFrames;
            if (frames.HasAnimation(animatedSprite.Animation))
                return frames.GetFrameTexture(animatedSprite.Animation, animatedSprite.Frame);
        }

        return null;
    }

    private void SetupGhostPool()
    {
        var max = Math.Max(0, MaxGhosts);
        _ghostBodies = new StaticBody3D[max];
        _ghostSprites = new Sprite3D[max];

        var playerShape = _playerCollisionShape?.Shape;

        for (var i = 0; i < max; i++)
        {
            var body = new StaticBody3D
            {
                CollisionLayer = 0,
                CollisionMask = 0
            };

            var shapeNode = new CollisionShape3D();
            if (playerShape is not null)
                shapeNode.Shape = (Shape3D)playerShape.Duplicate();
            body.AddChild(shapeNode);

            var sprite = new Sprite3D
            {
                Centered = true,
                Visible = false,
                RenderPriority = -1,
                TopLevel = true
            };
            body.AddChild(sprite);

            AddChild(body);

            _ghostBodies[i] = body;
            _ghostSprites[i] = sprite;
        }
    }

    private void SetupFixedGhostPool()
    {
        var max = Math.Max(0, MaxFixedGhosts);
        _fixedGhostBodies = new StaticBody3D[max];
        _fixedGhostSprites = new Sprite3D[max];
        _fixedGhostShapes = new CollisionShape3D[max];
        _fixedGhostPendingEnable = new bool[max];
        _fixedGhostRemaining = new double[max];

        var playerShape = _playerCollisionShape?.Shape;

        for (var i = 0; i < max; i++)
        {
            var body = new StaticBody3D
            {
                CollisionLayer = 0,
                CollisionMask = 0
            };

            var shapeNode = new CollisionShape3D
            {
                Disabled = true
            };
            if (playerShape is not null)
                shapeNode.Shape = (Shape3D)playerShape.Duplicate();
            body.AddChild(shapeNode);

            var sprite = new Sprite3D
            {
                Centered = true,
                Visible = false
            };
            body.AddChild(sprite);

            AddChild(body);
            body.Visible = false;

            _fixedGhostBodies[i] = body;
            _fixedGhostSprites[i] = sprite;
            _fixedGhostShapes[i] = shapeNode;
            _fixedGhostPendingEnable[i] = false;
            _fixedGhostRemaining[i] = 0.0;
        }
    }

    /// <summary>
    /// Creates a fixed ghost from the current first dynamic ghost.
    /// </summary>
    public bool CreateFixedGhost()
    {
        if (_ghostBodies.Length == 0 || !_ghostBodies[0].Visible)
        {
            GD.PushError("没有可用的动态残影，无法创建固定残影");
            return false;
        }

        for (var i = 0; i < _fixedGhostBodies.Length; i++)
        {
            var body = _fixedGhostBodies[i];
            if (body.Visible)
                continue;

            var sprite = _fixedGhostSprites[i];
            var sourceBody = _ghostBodies[0];
            var sourceSprite = _ghostSprites[0];

            body.GlobalPosition = sourceBody.GlobalPosition;
            body.Scale = sourceBody.Scale;

            if (sourceSprite.Texture is not null)
                sprite.Texture = sourceSprite.Texture;
            else
                sprite.Texture = GetPlayerTexture();

            var modulate = sprite.Modulate;
            modulate.A = 1.0f;
            sprite.Modulate = modulate;

            body.Visible = true;
            sprite.Visible = true;

            if (IsFixedGhostOverlappingPlayer(i))
            {
                EnableCollision(body, false);
                _fixedGhostPendingEnable[i] = true;
            }
            else
            {
                EnableCollision(body, true);
                _fixedGhostPendingEnable[i] = false;
            }

            _fixedGhostRemaining[i] = FixedGhostLifetime;
            EventHub.Instance?.Publish(new FixedGhostCreatedEvent(body.GlobalPosition, FixedGhostLifetime));
            return true;
        }

        GD.PushError("固定残影池已满，无法创建新的固定残影");
        return false;
    }

    private bool IsFixedGhostOverlappingPlayer(int index)
    {
        var body = _fixedGhostBodies[index];
        var shapeNode = _fixedGhostShapes[index];
        if (shapeNode?.Shape is null)
            return false;

        var spaceState = GetWorld3D()?.DirectSpaceState;
        if (spaceState is null)
            return false;

        var query = new PhysicsShapeQueryParameters3D
        {
            Shape = shapeNode.Shape,
            Transform = body.GlobalTransform,
            CollideWithBodies = true,
            CollideWithAreas = false,
            CollisionMask = 1
        };
        query.Exclude = new Godot.Collections.Array<Rid> { body.GetRid() };

        var results = spaceState.IntersectShape(query, 32);
        foreach (var result in results)
        {
            var collider = result["collider"].As<GodotObject>();
            if (collider == Player)
                return true;
        }

        return false;
    }

    private void EnableCollision(StaticBody3D body, bool enabled)
    {
        foreach (var child in body.GetChildren())
        {
            if (child is CollisionShape3D shape)
                shape.Disabled = !enabled;
        }

        body.CollisionLayer = enabled ? 1u : 0u;
    }

    private void UpdateFixedGhosts(double delta)
    {
        for (var i = 0; i < _fixedGhostBodies.Length; i++)
        {
            if (!_fixedGhostBodies[i].Visible)
                continue;

            _fixedGhostRemaining[i] -= delta;
            if (_fixedGhostRemaining[i] <= 0.0)
                ExpireFixedGhost(i);
        }
    }

    private void ExpireFixedGhost(int index)
    {
        var body = _fixedGhostBodies[index];
        EnableCollision(body, false);
        body.Visible = false;
        _fixedGhostSprites[index].Visible = false;
        _fixedGhostRemaining[index] = 0.0;
        _fixedGhostPendingEnable[index] = false;

        EventHub.Instance?.Publish(new FixedGhostExpiredEvent(body.GlobalPosition));
    }

    private void CheckPendingFixedGhosts()
    {
        for (var i = 0; i < _fixedGhostBodies.Length; i++)
        {
            if (!_fixedGhostPendingEnable[i])
                continue;

            if (!_fixedGhostBodies[i].Visible)
            {
                _fixedGhostPendingEnable[i] = false;
                continue;
            }

            if (!IsFixedGhostOverlappingPlayer(i))
            {
                EnableCollision(_fixedGhostBodies[i], true);
                _fixedGhostPendingEnable[i] = false;
            }
        }
    }

    public void SetHistoryLength(int value)
    {
        var newLength = Math.Max(1, value);
        HistoryLength = newLength;

        if (_history.Length == newLength)
            return;

        var newHistory = new GhostState[newLength];
        var keep = Math.Min(_historyCount, newLength);
        if (_history.Length > 0 && keep > 0)
        {
            for (var i = 0; i < keep; i++)
            {
                var srcIndex = (_historyHead + _historyCount - keep + i) % _history.Length;
                newHistory[i] = _history[srcIndex];
            }
        }

        _history = newHistory;
        _historyHead = 0;
        _historyCount = keep;
    }

    private void HideAllDynamicSprites()
    {
        foreach (var sprite in _ghostSprites)
            sprite.Visible = false;
    }

    private void HideAllGhosts()
    {
        foreach (var body in _ghostBodies)
            body.Visible = false;

        foreach (var sprite in _ghostSprites)
            sprite.Visible = false;
    }

    private void HistoryAdd(in GhostState state)
    {
        if (_history.Length == 0)
            _history = new GhostState[Math.Max(1, HistoryLength)];

        if (_historyCount < _history.Length)
        {
            _history[(_historyHead + _historyCount) % _history.Length] = state;
            _historyCount++;
        }
        else
        {
            _history[_historyHead] = state;
            _historyHead = (_historyHead + 1) % _history.Length;
        }
    }

    private GhostState GetHistoryFromOldest(int index)
    {
        return _history[(_historyHead + index) % _history.Length];
    }

    private void RegisterConsoleCommands()
    {
        LimboConsole.RegisterCommand(new Callable(this, MethodName.CmdPlayerLatency), "player_latency");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.CmdSetHistoryLength), "player_latency set_length");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.CmdSetStepBetweenGhosts), "player_latency set_step");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.CmdSetVisibleGhosts), "player_latency set_visible", "设置可见残影数量 (0~max_ghosts)");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.CmdCreateFixedGhost), "player_latency create_fixed", "在当前玩家位置创建一个固定残影（技能）");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.CmdSetFixedGhostLifetime), "player_latency set_fixed_lifetime", "设置固定残影的持续时间（秒）");
    }

    private void CmdPlayerLatency()
    {
        LimboConsole.Info(
            $"ENABLED: {_latencyController?.IsEnabled ?? false}\nINTERVAL: {_latencyController?.LatencySecs ?? 0f}\nMAX_GHOSTS: {MaxGhosts}\nVISIBLE_GHOSTS: {VisibleGhosts}\nHISTORY_LENGTH: {HistoryLength}\nSTEP: {StepBetweenGhosts}");
    }

    private void CmdSetHistoryLength(int value)
    {
        SetHistoryLength(value);
    }

    private void CmdSetStepBetweenGhosts(float value)
    {
        StepBetweenGhosts = value;
    }

    private void CmdSetVisibleGhosts(int value)
    {
        VisibleGhosts = Math.Clamp(value, 0, Math.Max(0, MaxGhosts));
    }

    private void CmdCreateFixedGhost()
    {
        CreateFixedGhost();
    }

    private void CmdSetFixedGhostLifetime(float value)
    {
        FixedGhostLifetime = Math.Max(value, 0.5f);
    }

    private void SetupDebugMarkers()
    {
        _debugMarkers = new MeshInstance3D[DebugDrawMax];

        var sphereMesh = new SphereMesh
        {
            Radius = DebugMarkerSize,
            Height = DebugMarkerSize * 2,
            Material = GetDebugMaterial()
        };

        for (var i = 0; i < _debugMarkers.Length; i++)
        {
            var marker = new MeshInstance3D
            {
                Mesh = sphereMesh,
                Visible = false
            };
            AddChild(marker);
            _debugMarkers[i] = marker;
        }
    }

    private void SetupDebugLabels()
    {
        _debugLabels = new Label3D[DebugDrawMax];

        for (var i = 0; i < _debugLabels.Length; i++)
        {
            var label = new Label3D
            {
                Text = "",
                FontSize = 10,
                OutlineSize = 2,
                OutlineModulate = new Color(0, 0, 0, 1),
                PixelSize = 0.02f,
                Modulate = new Color(1, 1, 0, 1),
                Visible = false
            };
            AddChild(label);
            _debugLabels[i] = label;
        }
    }

    private StandardMaterial3D GetDebugMaterial()
    {
        if (_debugMaterial is not null)
            return _debugMaterial;

        _debugMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(1, 0, 0, 0.6f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
        };
        return _debugMaterial;
    }

    private void UpdateDebugMarkers()
    {
        var total = _historyCount;
        var count = Math.Min(total, DebugDrawMax);

        // i = 0 is the newest sample, matching the GDS debug draw order.
        for (var i = 0; i < count; i++)
        {
            var state = GetHistoryFromOldest(total - 1 - i);

            var marker = _debugMarkers[i];
            marker.GlobalPosition = state.Position;
            marker.Visible = true;

            var label = _debugLabels[i];
            label.GlobalPosition = state.Position + new Vector3(0, DebugMarkerSize * 2, 0);

            var parts = new List<string>(2);
            if (DebugShowIndex)
                parts.Add($"[{total - 1 - i}]");
            if (DebugShowTime)
                parts.Add($"{state.Timestamp:0.00}");
            label.Text = string.Join(" ", parts);
            label.Visible = true;
        }

        for (var i = count; i < _debugMarkers.Length; i++)
        {
            _debugMarkers[i].Visible = false;
            _debugLabels[i].Visible = false;
        }
    }
}
