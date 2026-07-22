extends Node

const SAVE_FILE = "user://player_save"

# ===== 信号 =====
signal var_changed(key: String, new_value: Variant)

# ===== 存储字典 =====
var _data: Dictionary = {}

# ===== 初始化（可选） =====
func _ready() -> void:
	# 在这里设置默认值，或从配置文件加载
	reset_defaults()

func reset_defaults() -> void:
	_data = {
	}
	# 发出变更信号（可选）
	for key in _data.keys():
		var_changed.emit(key, _data[key])

# ===== 设置变量 =====
func set_var(key: String, value: Variant) -> void:
	if _data.has(key) and _data[key] == value:
		return  # 值相同就不触发信号
	_data[key] = value
	var_changed.emit(key, value)

# ===== 获取变量（可指定默认值） =====
func get_var(key: String, default: Variant = null) -> Variant:
	if _data.has(key):
		return _data[key]
	return default

# ===== 检查变量是否存在 =====
func has_var(key: String) -> bool:
	return _data.has(key)

# ===== 删除变量 =====
func erase_var(key: String) -> void:
	if _data.has(key):
		_data.erase(key)
		var_changed.emit(key, null)  # 传递 null 表示已删除

# ===== 获取所有数据（用于存档） =====
func get_all_data() -> Dictionary:
	return _data.duplicate(true)  # 深拷贝

# ===== 批量设置（用于读档） =====
func set_all_data(data: Dictionary) -> void:
	_data = data.duplicate(true)
	for key in data.keys():
		var_changed.emit(key, data[key])

func save_to_json() -> void:
	print("REQUEST SAVE")
	_data["last_saved"] = Time.get_datetime_string_from_system(true)
	
	var file = FileAccess.open(SAVE_FILE, FileAccess.WRITE)
	if file:
		file.store_string(JSON.stringify(_data))
		file.close()

func load_from_json() -> void:
	print("REQUEST LOAD")
	if not FileAccess.file_exists(SAVE_FILE):
		return
	var file = FileAccess.open(SAVE_FILE, FileAccess.READ)
	if file:
		var text = file.get_as_text()
		var json = JSON.parse_string(text)
		if json is Dictionary:
			_data = json
			print("LOAD SUCCESS")
	return
