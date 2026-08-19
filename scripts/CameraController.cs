using Godot;
using Microsoft.Extensions.Logging;
using PhantomCamera;
using PhantomCamera.Manager;

namespace Circumlink;

public partial class CameraController : Node
{
    [Export]
    private NodePath _playerCameraPath;

    private PhantomCamera3D _playerCamera;

    private ushort _playerCameraPriority = 50;

    private ushort _cameraFocusPriority = 100;

    private ILogger<CameraController> _logger = Debug.Log.GetLogger<CameraController>();

    public override void _Ready()
    {
        _playerCamera = GetNode<Node3D>(_playerCameraPath).AsPhantomCamera3D();
        _playerCamera.Priority = _playerCameraPriority;

        _logger.LogInformation("CameraController ready.");
    }
}
