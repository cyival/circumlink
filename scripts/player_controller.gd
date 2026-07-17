extends CharacterBody3D

# ===== 可调节参数 =====
@export var walk_speed: float = 6.0
@export var jump_velocity: float = 10.0
@export var gravity: float = 15.0
@export var acceleration: float = 12.0    # 地面加速度
@export var air_acceleration: float = 4.0 # 空中加速度
@export var friction: float = 10.0        # 地面摩擦减速

# ===== 内部变量 =====
var current_acceleration: float = acceleration

func _physics_process(delta: float) -> void:
	# ----- 1. 重力（始终向下）-----
	if not is_on_floor():
		velocity.y -= gravity * delta

	# ----- 2. 获取左右输入（X轴方向）-----
	var horizontal := Input.get_axis("move_left", "move_right")  # -1 左，1 右

	# ----- 3. 选择加速度（地面/空中）-----
	if is_on_floor():
		current_acceleration = acceleration
	else:
		current_acceleration = air_acceleration

	# ----- 4. 应用水平移动（X轴）-----
	if horizontal != 0:
		# 向目标速度加速（目标速度 = 水平输入 * 速度值）
		var target_velocity = horizontal * walk_speed
		velocity.x = move_toward(velocity.x, target_velocity, current_acceleration * delta)
	else:
		# 摩擦力：水平减速至0
		velocity.x = move_toward(velocity.x, 0.0, friction * delta)

	# ----- 5. 确保Z轴永远为0（固定平面）-----
	velocity.z = 0.0

	# ----- 6. 跳跃（仅在地面）-----
	if Input.is_action_just_pressed("jump") and is_on_floor():
		velocity.y = jump_velocity

	# ----- 7. 移动并处理碰撞-----
	move_and_slide()
