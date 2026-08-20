using Godot;
using Microsoft.Extensions.Logging;
using PhantomCamera;
using PhantomCamera.Manager;

namespace Circumlink;

public partial class CameraController : Node
{
    private ILogger<CameraController> _logger = Debug.Log.GetLogger<CameraController>();

    [Export]
    private NodePath _playerCameraPath;

    [Export]
    private NodePath _subCameraPath;

    private PhantomCamera3D _playerCamera;

    private PhantomCamera3D _subCamera;

    private ushort _playerCameraPriority = 50;

    private ushort _cameraFocusPriority = 100;

    public Vector3 DefaultSubCameraOffset = new(3, 0, 5);

    public override void _Ready()
    {
        _playerCamera = GetNode<Node3D>(_playerCameraPath).AsPhantomCamera3D();
        _playerCamera.Priority = _playerCameraPriority;

        _subCamera = GetNode<Node3D>(_subCameraPath).AsPhantomCamera3D();
        _subCamera.Priority = 0;
        _subCamera.FollowOffset = DefaultSubCameraOffset;

        _logger.LogInformation("CameraController ready.");
    }

    public void UseSubCamera(bool focus = true) => _subCamera.Priority = focus ? _cameraFocusPriority : 0;

    public void FocusSubCameraOn(Node3D target) => _subCamera.FollowTarget = target;

    public void SetSubCameraOffset(Vector3 offset) => _subCamera.FollowOffset = offset;

    public void ResetSubCamera()
    {
        _subCamera.FollowTarget = null;
        _subCamera.FollowOffset = DefaultSubCameraOffset;
    }
}
