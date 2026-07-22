extends Node3D

const LEVELS_JSON = "res://levels.json"

## 需要被传送的玩家节点
@export var player: Node3D

## 当场景中没有 PlayerSpawn 时使用的默认出生位置
@export var default_spawn_position := Vector3.ZERO

## 关卡名称 -> 场景路径
var _registered_levels: Dictionary = {}
var _current_level: Node3D
var _current_level_name: StringName = ""

func _ready() -> void:
	if not player:
		printerr("Level Controller: Player 未设置！")
	_load_registry_from_json()
	_register_console_commands()

func _load_registry_from_json() -> void:
	if not FileAccess.file_exists(LEVELS_JSON):
		push_warning("Level registry file not found: ", LEVELS_JSON)
		return

	var file = FileAccess.open(LEVELS_JSON, FileAccess.READ)
	if file == null:
		push_error("Failed to open level registry: ", LEVELS_JSON)
		return

	var text = file.get_as_text()
	var json = JSON.parse_string(text)
	if json == null or not json is Dictionary:
		push_error("Invalid level registry JSON in ", LEVELS_JSON)
		return

	# 合并到现有注册表（支持手动添加的项）
	for key in json.keys():
		_registered_levels[StringName(key)] = json[key]

## 手动注册一个关卡
func register_level(name: StringName, scene_path: String) -> void:
	_registered_levels[name] = scene_path

## 取消注册
func unregister_level(name: StringName) -> void:
	_registered_levels.erase(name)

## 通过名称加载关卡
func load_level_by_name(name: StringName) -> bool:
	if not _registered_levels.has(name):
		push_error("Level not registered: ", name)
		return false
	return load_level(_registered_levels[name])

## 通过路径加载关卡，返回是否成功
func load_level(scene_path: String) -> bool:
	var res: PackedScene = load(scene_path) as PackedScene
	if res == null:
		push_error("Failed to load scene: ", scene_path)
		return false

	var node = res.instantiate()
	if node == null:
		push_error("Failed to instantiate scene: ", scene_path)
		return false

	# 移除旧关卡
	if _current_level:
		_current_level.queue_free()

	# 挂载新关卡
	_current_level = node
	add_child(node)

	# 记录当前关卡名称（如果有注册名）
	_current_level_name = _find_name_by_path(scene_path)
	
	_reset_player_position()
	
	return true

## 获取当前关卡实例
func get_current_level() -> Node3D:
	return _current_level

## 获取当前关卡名称（如果有注册）
func get_current_level_name() -> StringName:
	return _current_level_name

## 重新加载当前关卡
func reload_current() -> bool:
	if _current_level == null:
		return false
	var path = _find_path_by_instance(_current_level)
	if path.is_empty():
		return false
	return load_level(path)

## 辅助：根据路径查找注册名
func _find_name_by_path(path: String) -> StringName:
	for name in _registered_levels:
		if _registered_levels[name] == path:
			return name
	return ""

## 辅助：根据实例查找它的场景路径（粗略实现）
func _find_path_by_instance(node: Node) -> String:
	if node.scene_file_path:
		return node.scene_file_path
	# 或者通过资源路径查找，备选
	return ""

## 重置玩家到当前关卡的出生点（PlayerSpawn），若没有则使用默认位置
func _reset_player_position() -> void:
	if not player or not _current_level:
		return

	# 在当前关卡中查找名为 "PlayerSpawn" 的节点
	var spawn_node = _current_level.find_child("PlayerSpawn", true, false)  # 递归，不拥有
	if spawn_node and spawn_node is Node3D:
		player.global_position = (spawn_node as Node3D).global_position
	else:
		# 回退到默认位置（可以是 Vector3.ZERO 或你配置的默认值）
		player.global_position = default_spawn_position

	# 如果玩家是 CharacterBody3D，可酌情重置速度和物理状态
	if player is CharacterBody3D:
		(player as CharacterBody3D).velocity = Vector3.ZERO
		# 可选：消除剩余的碰撞重叠
		# (player as CharacterBody3D).move_and_slide()

func _register_console_commands() -> void:
	# 列出所有已注册的关卡
	LimboConsole.register_command(_cmd_list_levels, "level list", "List all registered levels")

	# 通过名称加载关卡
	LimboConsole.register_command(
		func(name: String): load_level_by_name(name),
		"level load",
		"Load a level by name. Usage: level load <name>"
	)

	# 重新加载当前关卡
	LimboConsole.register_command(
		func(): reload_current(),
		"level reload",
		"Reload the current level"
	)

	# 手动注册一个关卡（运行时添加）
	LimboConsole.register_command(
		func(name: String, path: String): register_level(name, path),
		"level register",
		"Register a new level at runtime. Usage: level register <name> <path>"
	)

	# 取消注册
	LimboConsole.register_command(
		func(name: String): unregister_level(name),
		"level unregister",
		"Unregister a level. Usage: level unregister <name>"
	)

	# 显示当前关卡名称和路径
	LimboConsole.register_command(_cmd_level_status, "level status", "Show current level info")

	# 通过路径直接加载（不依赖注册表）
	LimboConsole.register_command(
		func(path: String): load_level(path),
		"level loadpath",
		"Load a level by scene path directly. Usage: level loadpath <res://scenes/my_level.tscn>"
	)

func _cmd_list_levels() -> void:
	var msg = "Registered levels:\n"
	for name in _registered_levels:
		msg += "  %s -> %s\n" % [name, _registered_levels[name]]
	LimboConsole.info(msg)

func _cmd_level_status() -> void:
	if _current_level == null:
		LimboConsole.info("No level is currently loaded.")
	else:
		var path = _current_level.scene_file_path if _current_level.scene_file_path else "unknown"
		LimboConsole.info("Current level: %s (path: %s)" % [_current_level_name, path])
