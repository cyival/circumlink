extends Node3D

# TODO: Jitter improvements

@export var player: CharacterBody3D
@export var max_ghosts: int = 10
@export var fade_duration: float = 0.8   # 单个残影淡出时间（秒）
@export var history_length: int = 200    # 历史容量（足够长）
@export var step_between_ghosts: float = 0.05  # 每个 ghost 之间的时间间隔（秒）
@export var visible_ghosts: int = 1   # 屏幕上实际显示的残影数量（≤ max_ghosts）
@export var sync_steps_with_latency: bool = true

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
var _ghost_pool: Array[Sprite3D] = []
var _sample_timer: float = 0.0
var _sample_interval: float = 0.02   # 采样间隔（50Hz）

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
	_register_commands()
	if debug_draw_positions:
		_setup_debug_markers()
		_setup_debug_labels()

func _setup_ghost_pool():
	for i in range(max_ghosts):
		var ghost = Sprite3D.new()
		ghost.centered = true
		ghost.visible = false
		add_child(ghost)
		_ghost_pool.append(ghost)

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
		#var state = _find_closest_state(target_time)
		var state = _interpolate_state(target_time)
		if state:
			var ghost = _ghost_pool[i]
			ghost.global_position = state.position
			ghost.scale = state.scale
			if state.texture:
				ghost.texture = state.texture
			ghost.visible = true
			# 透明度：从新到旧逐渐变淡
			var alpha = 1.0 - float(i) / ghost_count
			ghost.modulate.a = clamp(alpha, 0.1, 1.0)
		else:
			_ghost_pool[i].visible = false
	
	# 隐藏多余的 ghost
	for i in range(ghost_count, _ghost_pool.size()):
		_ghost_pool[i].visible = false
		
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

# 插值 + 二分
func _interpolate_state(target_time: float) -> GhostState:
	if _history.is_empty():
		return null
	if _history.size() == 1:
		return _history[0]
	
	# 二分查找目标时间所在的区间
	var lo = 0
	var hi = _history.size() - 1
	while lo < hi:
		var mid = (lo + hi) / 2
		if _history[mid].timestamp < target_time:
			lo = mid + 1
		else:
			hi = mid
	
	# 找到邻近的两个状态（lo 和 lo-1）
	var idx0 = max(lo - 1, 0)
	var idx1 = min(lo, _history.size() - 1)
	var state0 = _history[idx0]
	var state1 = _history[idx1]
	
	# 如果两个状态时间相同，直接返回
	if state1.timestamp == state0.timestamp:
		return state0
	
	# 计算插值权重 (0~1)
	var t = (target_time - state0.timestamp) / (state1.timestamp - state0.timestamp)
	t = clamp(t, 0.0, 1.0)
	
	# 插值位置和缩放
	var pos = state0.position.lerp(state1.position, t)
	var scale = state0.scale.lerp(state1.scale, t)
	
	# 纹理和帧不插值，取最近的那个（或取 state1，一般差别不大）
	var tex = state1.texture if t > 0.5 else state0.texture
	var anim = state1.animation_name if t > 0.5 else state0.animation_name
	var frame = state1.frame if t > 0.5 else state0.frame
	
	# 返回插值后的状态（timestamp 设为 target_time 便于调试）
	return GhostState.new(pos, scale, tex, anim, frame, target_time)

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
