extends Control

const GAME_SCENE = "res://scenes/game.tscn"

@export var warning: Control
@export var screen_effect: Control
@export var animation_player: AnimationPlayer
@export var level_to_be_loaded: String

var waiting_for_input: bool = false

func _ready() -> void:
	animation_player.animation_finished.connect(_on_animation_finished)
	
	if not FileAccess.file_exists(VarStore.SAVE_FILE):
		screen_effect.hide()
		warning.show()
		waiting_for_input = true
	else:
		VarStore.load_from_json()
		_animate_enter()

func _unhandled_key_input(event: InputEvent) -> void:
	if waiting_for_input and event.is_pressed():
		waiting_for_input = false
		VarStore.save_to_json()
		_animate_enter()

func _animate_enter() -> void:
	warning.hide()
	screen_effect.show()
	animation_player.play("start")

func _load_game() -> Node:
	var packed = ResourceLoader.load(GAME_SCENE) as PackedScene
	var node = packed.instantiate()
	get_tree().root.call_deferred("add_child", node)
	
	return node

func _on_animation_finished(anim_name: StringName) -> void:
	if anim_name == "start":
		var node = _load_game()
		
		var level_controller = node.get_node("LevelController")
		level_controller.ready.connect(func(): level_controller.load_level_by_name(level_to_be_loaded))
		
		queue_free()
