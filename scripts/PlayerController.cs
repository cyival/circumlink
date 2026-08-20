using Godot;

namespace Circumlink;

public partial class PlayerController : CharacterBody3D
{
    [Export]
    public float WalkSpeed = 6.0f;
    [Export]
    public float JumpVelocity = 10.0f;
    [Export]
    public float Gravity = 9.8f;
    [Export]
    public float Acceleration = 12.0f;    // 地面加速度
    [Export]
    public float AirAcceleration = 4.0f; // 空中加速度
    [Export]
    public float Friction = 10.0f;        // 地面摩擦减速

    public bool IsEnabled = false;
    public bool IsControlEnabled = true;

    private float _currnetAcceleration = 0.0f;


    public override void _PhysicsProcess(double delta)
    {
        if (!IsEnabled) return;

        // Gravity
        if (!IsOnFloor())
        {
            Velocity = Velocity with { Y = Velocity.Y - (float)delta * Gravity };
        }

        var horizontalInput = IsControlEnabled ? Input.GetAxis("move_left", "move_right") : 0;

        // Select acceleration based on ground or air state
        _currnetAcceleration = IsOnFloor() ? Acceleration : AirAcceleration;

        // Apply horizontal movement
        if (horizontalInput != 0)
        {
            // Apply acceleration to target velocity
            var targetVelocity = horizontalInput * WalkSpeed;
            Velocity = Velocity with { X = Mathf.MoveToward(Velocity.X, targetVelocity, _currnetAcceleration) };
        }
        else
        {
            // Friction
            Velocity = Velocity with { X = Mathf.MoveToward(Velocity.X, 0, Friction * (float)delta) };
        }

        // Make sure Z is zero
        Velocity = Velocity with { Z = 0 };

        // Jump (only when on the ground)
        if (IsControlEnabled && Input.IsActionJustPressed("jump") && IsOnFloor())
        {
            Velocity = Velocity with { Y = JumpVelocity };
        }

        // Finish up
        MoveAndSlide();
    }
}
