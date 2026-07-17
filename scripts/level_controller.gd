extends Node3D

const LEVELS_JSON = "res://levels.json"

var level_path: String = ""

var _registered_levels: Dictionary[StringName, String] = {}
var _current_level: Node3D

func _ready() -> void:
	var pre_registered_levels = JSON.parse_string(FileAccess.get_file_as_string(LEVELS_JSON)) \
		as Dictionary[StringName, String]
	_registered_levels.merge(pre_registered_levels, true)

func load_level(scene_path: String) -> void:
	var res = ResourceLoader.load(scene_path) as PackedScene
	var node = res.instantiate()
	
	if _current_level:
		_current_level.queue_free()
	_current_level = node
	add_child(node)

func register_level(name: StringName, scene_path: String) -> void:
	_registered_levels[name] = scene_path
