extends Node3D

# TODO: Jitter improvements

# Godot does not currently support scaling of physics bodies or collision shapes. As a workaround, change the collision shape's extents instead of changing its scale. If you want the visual representation's scale to change as well, change the scale of the underlying visual representation (Sprite2D, MeshInstance3D, …) and change the collision shape's extents separately. Make sure the collision shape is not a child of the visual representation in this case.

@export var player: CharacterBody3D
@export var max_ghosts: int = 10
@export var fade_duration: float = 0.8   # 单个残影淡出时间（秒）
@export var history_length: int = 200    # 历史容量（足够长）
@export var step_between_ghosts: float = 0.05  # 每个 ghost 之间的时间间隔（秒）
@export var visible_ghosts: int = 1   # 屏幕上实际显示的残影数量（≤ max_ghosts）
@export var sync_steps_with_latency: bool = true
@export var fixed_ghost_lifetime: float = 3.0   # 固定残影存在时间（秒）
@export var max_fixed_ghosts: int = 10          # 最大同时存在的固定残影数量
@export var ghost_start_alpha: float = 0.7

var interval: float = LatencyController.latency
var enabled: bool = true

# 内部类，用于状态管理
class GhostState:
	var position: Vector3
	var scale: Vector3
	var texture: Texture2D
	var animation_name: String
	var frame: int
	var timestamp: float   # 游戏时间（秒）

	func _init(p: Vector3, s: Vector3, tex: Texture2D = null, anim: String = "", f: int = 0, ts: float = 0.0):
		position = p
		scale = s
		texture = tex
		animation_name = anim
		frame = f
		timestamp = ts

# -- 内部变量 --
var _history: Array[GhostState] = []
var _ghost_pool: Array[StaticBody3D] = []
var _ghost_sprites: Array[Sprite3D] = []
var _fixed_ghost_pool: Array[StaticBody3D] = []  # 存储固定残影的物理体
var _fixed_ghost_sprites: Array[Sprite3D] = []   # 对应的精灵
var _fixed_ghost_timers: Array[Timer] = []       # 每个固定残影的倒计时
var _sample_timer: float = 0.0
var _sample_interval: float = 0.02   # 采样间隔（50Hz）
# 存储每个固定残影的碰撞形状节点引用
var _fixed_ghost_collision_shapes: Array[CollisionShape3D] = []
# 标记哪些固定残影正在等待“玩家离开后启用碰撞”
var _fixed_ghost_pending_enable: Array[bool] = []

func _record_state():
	var tex: Texture2D = null
	var anim_name: String = ""
	var frame: int = 0
	
	for child in player.get_children():
		if child is Sprite3D:
			tex = child.texture
			break
		elif child is AnimatedSprite3D:
			anim_name = child.animation
			frame = child.frame
			break
			
	var state = GhostState.new(
		player.global_position,
		player.scale,
		tex,
		anim_name,
		frame,
		Time.get_ticks_msec() / 1000.0   # 当前时间戳（秒）
	)
	_history.append(state)

func _ready():
	_setup_ghost_pool()
	_setup_fixed_ghost_pool()
	_register_commands()
	if debug_draw_positions:
		_setup_debug_markers()
		_setup_debug_labels()

func _setup_ghost_pool():
	var player_shape = _get_player_collision_shape()
	if not player_shape:
		printerr("未找到玩家的 CollisionShape3D，残影将无法产生碰撞！")
		
	for i in range(max_ghosts):
		var ghost_body = StaticBody3D.new()
		ghost_body.collision_layer = 0          # 设置在物理层1（需与玩家的mask匹配）
		ghost_body.collision_mask = 0           # 残影不需要主动检测别人，只负责被撞
		
		var shape_node = CollisionShape3D.new()
		if player_shape:
			shape_node.shape = player_shape.duplicate()  # 必须复制，避免共享同一资源
		ghost_body.add_child(shape_node)
		
		var sprite = Sprite3D.new()
		sprite.centered = true
		sprite.visible = false
		sprite.render_priority = -1                      # 避免渲染与玩家 Sprite 冲突
		ghost_body.add_child(sprite)
		
		add_child(ghost_body)
		_ghost_pool.append(ghost_body)
		_ghost_sprites.append(sprite)

func _setup_fixed_ghost_pool():
	var player_shape = _get_player_collision_shape()
	for i in range(max_fixed_ghosts):
		var ghost_body = StaticBody3D.new()
		ghost_body.collision_layer = 0
		ghost_body.collision_mask = 0
		
		var shape_node = CollisionShape3D.new()
		if player_shape:
			shape_node.shape = player_shape.duplicate()
		# 默认禁用
		shape_node.disabled = true
		ghost_body.add_child(shape_node)
		
		var sprite = Sprite3D.new()
		sprite.centered = true
		sprite.visible = false
		ghost_body.add_child(sprite)
		
		add_child(ghost_body)
		
		# 创建对应的 Timer（作为子节点）
		var timer = Timer.new()
		timer.one_shot = true
		timer.timeout.connect(_on_fixed_ghost_timeout.bind(ghost_body, sprite, timer))
		ghost_body.add_child(timer)  # 将 timer 作为残影子节点，方便管理
		
		_fixed_ghost_pool.append(ghost_body)
		_fixed_ghost_sprites.append(sprite)
		_fixed_ghost_timers.append(timer)
		
		# 保存碰撞形状引用
		_fixed_ghost_collision_shapes.append(shape_node)
		_fixed_ghost_pending_enable.append(false)
		
		ghost_body.visible = false

func _is_fixed_ghost_overlapping_player(index: int) -> bool:
	var body = _fixed_ghost_pool[index]
	var shape_node = _fixed_ghost_collision_shapes[index]
	if not shape_node or not shape_node.shape:
		return false

	var space_state = get_world_3d().direct_space_state
	var query = PhysicsShapeQueryParameters3D.new()
	query.shape = shape_node.shape
	query.transform = body.global_transform      # 形状跟随物理体
	query.collide_with_bodies = true
	query.collide_with_areas = false
	query.collision_mask = 1                     # 玩家所在的层
	query.exclude = [body.get_rid()]             # 排除自身

	var results = space_state.intersect_shape(query)
	for result in results:
		if result.collider == player:
			return true
	return false

func _get_player_collision_shape() -> Shape3D:
	if not player:
		return null
	# 遍历玩家子节点，找到第一个 CollisionShape3D
	for child in player.get_children():
		if child is CollisionShape3D:
			return child.shape
	return null

func _get_player_texture() -> Texture2D:
	for child in player.get_children():
		if child is Sprite3D:
			return child.texture
		elif child is AnimatedSprite3D:
			# 获取当前帧纹理（可能需要额外处理）
			var anim_sprite = child as AnimatedSprite3D
			if anim_sprite.sprite_frames:
				var frames = anim_sprite.sprite_frames
				if frames.has_animation(anim_sprite.animation):
					return frames.get_frame_texture(anim_sprite.animation, anim_sprite.frame)
	return null

# ===== 物理帧更新 =====
func _physics_process(delta):
	if not enabled or not player:
		return
	
	# 确保 visible_ghosts 不超过池大小
	var display_count = min(visible_ghosts, max_ghosts)
	if display_count <= 0:
		hide_all_ghosts()
		return
	
	interval = LatencyController.get_latency()
	if interval < 0.2:
		# 延迟太小，隐藏所有 ghost
		hide_all_ghosts()
		return
	
	if sync_steps_with_latency:
		step_between_ghosts = interval

	# 采样（固定间隔）
	_sample_timer += delta
	if _sample_timer >= _sample_interval:
		_sample_timer = 0.0
		#_sample_timer -= _sample_interval
		_record_state()
	
	# 限制历史长度
	while _history.size() > history_length:
		_history.pop_front()
	
	# 当前时间
	var now = Time.get_ticks_msec() / 1000.0
	
	# 计算每个 ghost 的目标时间
	# ghost 0（最新）：延迟 interval
	# ghost i：延迟 interval + i * step_between_ghosts
	var ghost_count = min(display_count, _history.size())
	for i in range(ghost_count):
		var target_time = now - interval - i * step_between_ghosts
		# 获取物理体和精灵
		var ghost_body = _ghost_pool[i]
		var ghost_sprite = _ghost_sprites[i]
		
		var success := _apply_interpolated_state(ghost_body, ghost_sprite, target_time)
		if success:
			# 设置透明度（基于索引的渐变）
			var alpha = ghost_start_alpha * (1 - float(i) / ghost_count)
			ghost_sprite.modulate.a = clamp(alpha, 0.1, 1.0)
		else:
			ghost_body.visible = false
	
	# 隐藏多余的 ghost
	for i in range(ghost_count, _ghost_pool.size()):
		_ghost_pool[i].visible = false
	
	_check_pending_fixed_ghosts()
	
	if debug_draw_positions:
		_update_debug_markers()

func hide_all_ghosts():
	for ghost in _ghost_pool:
		ghost.visible = false

# 在历史中查找最接近目标时间的状态（线性搜索，历史较短）
func _find_closest_state(target_time: float) -> GhostState:
	if _history.is_empty():
		return null
	var best_state = _history[0]
	var best_diff = abs(best_state.timestamp - target_time)
	for state in _history:
		var diff = abs(state.timestamp - target_time)
		if diff < best_diff:
			best_diff = diff
			best_state = state
	return best_state

# 插值 + 二分，直接将插值结果应用到指定的 ghost_body 和 ghost_sprite
func _apply_interpolated_state(ghost_body: StaticBody3D, ghost_sprite: Sprite3D, target_time: float) -> bool:
	if _history.is_empty():
		ghost_body.visible = false
		return false
	if _history.size() == 1:
		_apply_state_to_ghost(ghost_body, ghost_sprite, _history[0])
		return true

	# 二分查找区间
	var lo = 0
	var hi = _history.size() - 1
	while lo < hi:
		var mid = (lo + hi) / 2
		if _history[mid].timestamp < target_time:
			lo = mid + 1
		else:
			hi = mid

	var idx0 = max(lo - 1, 0)
	var idx1 = min(lo, _history.size() - 1)
	var state0 = _history[idx0]
	var state1 = _history[idx1]

	# 计算插值权重
	var t := 0.0
	var time_diff = state1.timestamp - state0.timestamp
	if time_diff > 0.0:
		t = clamp((target_time - state0.timestamp) / time_diff, 0.0, 1.0)

	# 插值位置和缩放
	var pos = state0.position.lerp(state1.position, t)
	var scale = state0.scale.lerp(state1.scale, t)

	# 纹理和帧取权重较大侧的状态（不插值）
	var tex: Texture2D = state1.texture if t > 0.5 else state0.texture
	var anim_name: String = state1.animation_name if t > 0.5 else state0.animation_name
	var frame: int = state1.frame if t > 0.5 else state0.frame

	# 直接应用到节点
	ghost_body.global_position = pos
	ghost_body.scale = scale
	if tex:
		ghost_sprite.texture = tex
	ghost_body.visible = true
	ghost_sprite.visible = true

	return true

func _apply_state_to_ghost(ghost_body: StaticBody3D, ghost_sprite: Sprite3D, state: GhostState):
	ghost_body.global_position = state.position
	ghost_body.scale = state.scale
	if state.texture:
		ghost_sprite.texture = state.texture
	ghost_body.visible = true
	ghost_sprite.visible = true

func create_fixed_ghost():
	# 检查是否有可用的动态残影（索引0必须存在且可见）
	if _ghost_pool.is_empty() or not _ghost_pool[0].visible:
		printerr("没有可用的动态残影，无法创建固定残影")
		return false
	
	# 从池中取一个可用的（未激活的）固定残影
	for i in range(max_fixed_ghosts):
		var body = _fixed_ghost_pool[i]
		if not body.visible:  # 如果当前是隐藏状态，则复用
			var sprite = _fixed_ghost_sprites[i]
			var timer = _fixed_ghost_timers[i]
			
			# 从第一个动态残影（索引0）获取位置和缩放
			var source_body = _ghost_pool[0]
			var source_sprite = _ghost_sprites[0]
			
			# 设置物理体位置和缩放
			body.global_position = source_body.global_position
			body.scale = source_body.scale
			
			# 复制精灵纹理（从源残影的精灵复制）
			if source_sprite.texture:
				sprite.texture = source_sprite.texture
			# 如果源残影没有纹理，尝试从玩家获取
			else:
				var tex = _get_player_texture()
				if tex:
					sprite.texture = tex
					
			sprite.modulate.a = 1.0   # 完全不透明
			
			# 显示
			body.visible = true
			sprite.visible = true
			
			if _is_fixed_ghost_overlapping_player(i):
				# 重叠：暂不启用碰撞，并标记为 pending
				_enable_collision(body, false)
				_fixed_ghost_pending_enable[i] = true
			else:
				# 不重叠：直接启用碰撞
				_enable_collision(body, true)
				_fixed_ghost_pending_enable[i] = false
			
			# 启动计时器
			timer.start(fixed_ghost_lifetime)
			
			return true
	
	printerr("固定残影池已满，无法创建新的固定残影")
	return false

func _enable_collision(body: StaticBody3D, enabled: bool):
	# 遍历子节点，找到 CollisionShape3D 并启用/禁用
	for child in body.get_children():
		if child is CollisionShape3D:
			child.disabled = not enabled
	# 同时设置碰撞层
	body.collision_layer = 1 if enabled else 0

func _on_fixed_ghost_timeout(body: StaticBody3D, sprite: Sprite3D, timer: Timer):
	var idx = _fixed_ghost_pool.find(body)
	if idx != -1:
		_fixed_ghost_pending_enable[idx] = false
	
	_enable_collision(body, false)
	# 隐藏残影
	body.visible = false
	sprite.visible = false
	# 停止计时器（已自动停止，但可重置）
	timer.stop()

func _check_pending_fixed_ghosts():
	for i in range(_fixed_ghost_pool.size()):
		if _fixed_ghost_pending_enable[i]:
			# 残影仍然可见（未超时）才检测
			if _fixed_ghost_pool[i].visible:
				if not _is_fixed_ghost_overlapping_player(i):
					# 玩家已离开，启用碰撞
					_enable_collision(_fixed_ghost_pool[i], true)
					_fixed_ghost_pending_enable[i] = false
			else:
				# 残影已被隐藏（例如超时），重置标记
				_fixed_ghost_pending_enable[i] = false

# ---- 控制台命令 ----
func _register_commands() -> void:
	LimboConsole.register_command(_cmd_info, "player_latency")
	LimboConsole.register_command(func(x: int): history_length = x, "player_latency set_length")
	LimboConsole.register_command(func(x: float): step_between_ghosts = x, "player_latency set_step")
	LimboConsole.register_command(
		func(x: int): visible_ghosts = clamp(x, 0, max_ghosts),
		"player_latency set_visible",
        "设置可见残影数量 (0~max_ghosts)"
	)
	LimboConsole.register_command(
		func(): create_fixed_ghost(),
		"player_latency create_fixed",
		"在当前玩家位置创建一个固定残影（技能）"
	)
	LimboConsole.register_command(
		func(x: float): fixed_ghost_lifetime = max(x, 0.5),
		"player_latency set_fixed_lifetime",
		"设置固定残影的持续时间（秒）"
	)

func _cmd_info() -> void:
	LimboConsole.info("ENABLED: %s\nINTERVAL: %s\nMAX_GHOSTS: %s\nVISIBLE_GHOSTS: %s\nHISTORY_LENGTH: %s\nSTEP: %s" % [
		enabled, interval, max_ghosts, visible_ghosts, history_length, step_between_ghosts
	])
	
# ---- DEBUG DRAWING ----
@export var debug_draw_positions: bool = false
@export var debug_draw_max: int = 30
@export var debug_marker_size: float = 0.05
@export var debug_show_index: bool = false
@export var debug_show_time: bool = false

var _debug_markers: Array[MeshInstance3D] = []
var _debug_labels: Array[Label3D] = []
var _debug_material: StandardMaterial3D

func _setup_debug_markers():
	var sphere_mesh = SphereMesh.new()
	sphere_mesh.radius = debug_marker_size
	sphere_mesh.height = debug_marker_size * 2
	sphere_mesh.material = _get_debug_material()
	for i in range(debug_draw_max):
		var marker = MeshInstance3D.new()
		marker.mesh = sphere_mesh
		marker.visible = false
		add_child(marker)
		_debug_markers.append(marker)

func _setup_debug_labels():
	for i in range(debug_draw_max):
		var label = Label3D.new()
		label.text = ""
		label.font_size = 10
		label.outline_size = 2
		label.outline_modulate = Color(0, 0, 0, 1)
		label.pixel_size = 0.02
		label.modulate = Color(1, 1, 0, 1)
		label.visible = false
		add_child(label)
		_debug_labels.append(label)

func _get_debug_material() -> StandardMaterial3D:
	if not _debug_material:
		_debug_material = StandardMaterial3D.new()
		_debug_material.albedo_color = Color(1, 0, 0, 0.6)
		_debug_material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
		_debug_material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	return _debug_material

func _update_debug_markers():
	var total = _history.size()
	var count = min(total, debug_draw_max)
	
	# 从最新开始（索引 total-1），依次往前显示
	for i in range(count):
		var state = _history[total - 1 - i]   # i=0 -> 最新，i=1 -> 次新
		var marker = _debug_markers[i]
		marker.global_position = state.position
		marker.visible = true
		
		var label = _debug_labels[i]
		label.global_position = state.position + Vector3(0, debug_marker_size * 2, 0)
		var parts = []
		if debug_show_index:
			parts.append("[%d]" % (total - 1 - i))   # 显示实际历史索引
		if debug_show_time:
			parts.append("%.2f" % state.timestamp)
		label.text = " ".join(parts)
		label.visible = true
	
	# 隐藏多余的
	for i in range(count, _debug_markers.size()):
		_debug_markers[i].visible = false
		_debug_labels[i].visible = false
