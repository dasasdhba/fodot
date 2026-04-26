@tool
extends EditorPlugin

# project settings

static func add_project_setting_info (key, type, hint, hint_str) :
	var info = {
		"name" : key,
		"type" : type,
		"hint" : hint,
		"hint_string" : hint_str
	}

	ProjectSettings.add_property_info(info)

static func add_project_setting(key, default, type, hint, hint_str) :
	if ProjectSettings.has_setting(key) :
		return

	ProjectSettings.set(key, default)
	add_project_setting_info(key, type, hint, hint_str)

const MAIN_SCENE_KEY = "fodot/general/main_scene"
const ASSEMBLY_KEY = "fodot/general/assemblies"
const BRIDGE_NAME = "Fodot"

func _enter_tree() -> void:
	add_project_setting_info(MAIN_SCENE_KEY, TYPE_STRING, PROPERTY_HINT_FILE, "*.tscn,*.scn,*.res")
	add_project_setting_info(ASSEMBLY_KEY, TYPE_STRING, PROPERTY_HINT_MULTILINE_TEXT, "")

func _enable_plugin() -> void:
	add_project_setting(MAIN_SCENE_KEY, "", TYPE_STRING, PROPERTY_HINT_FILE, "*.tscn,*.scn,*.res")
	add_project_setting(ASSEMBLY_KEY, "", TYPE_STRING, PROPERTY_HINT_MULTILINE_TEXT, "")
	add_autoload_singleton(BRIDGE_NAME, "Bridge.cs")

func _disable_plugin() -> void:
	ProjectSettings.clear(MAIN_SCENE_KEY)
	ProjectSettings.clear(ASSEMBLY_KEY)
	remove_autoload_singleton(BRIDGE_NAME)

# debug scene access

const DEBUG_FILE_PATH = "res://fodot_debug_scene"

var last_path : String

func _process(delta: float) -> void:
	var root = EditorInterface.get_edited_scene_root()
	if root == null: return

	var path = root.scene_file_path
	if path == last_path: return
	last_path = path

	var f = FileAccess.open(DEBUG_FILE_PATH, FileAccess.WRITE)
	f.store_string(path)
	f.close()