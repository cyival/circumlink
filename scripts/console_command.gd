extends Node

var noise_emitter: PhantomCameraNoiseEmitter3D

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	LimboConsole.register_command(LatencyController.get_latency, "latency", "Get latency")
	LimboConsole.register_command(func(x: float): LatencyController.latency = x, "latency set", "Set latency")
	LimboConsole.register_command(emit_noise, "emit_noise")
	
func emit_noise() -> void:
	if noise_emitter:
		noise_emitter.emit()
	else:
		LimboConsole.error("No PhantomCameraNoiseEmitter3D was configured")
