@tool
extends EditorPlugin

# project settings

static func add_project_setting_info(key, type, hint, hint_str) :
	var info = {
		"name" : key,
		"type" : type,
		"hint" : hint,
		"hint_string" : hint_str
	}

	ProjectSettings.add_property_info(info)

static func add_project_setting(key, default, type, hint, hint_str) :
	if !ProjectSettings.has_setting(key) :
		ProjectSettings.set(key, default)
	ProjectSettings.set_initial_value(key, default)
	add_project_setting_info(key, type, hint, hint_str)

const MAIN_SCENE_KEY = "fodot/general/main_scene"
const ASSEMBLY_KEY = "fodot/general/assemblies"
const LIBRARY_KEY = "fodot/general/library_schedule_time"
const BRIDGE_NAME = "Fodot"

func _enter_tree() -> void:
	add_project_setting(MAIN_SCENE_KEY, "", TYPE_STRING, PROPERTY_HINT_FILE, "*.tscn,*.scn,*.res")
	add_project_setting(ASSEMBLY_KEY, "", TYPE_STRING, PROPERTY_HINT_MULTILINE_TEXT, "")
	add_project_setting(LIBRARY_KEY, 3.0, TYPE_FLOAT, PROPERTY_HINT_RANGE, "0,60,0.5")
	EditorInterface.get_resource_filesystem().filesystem_changed.connect(_connect_to_filesystem)
	_thread_init()

func _exit_tree() -> void:
	EditorInterface.get_resource_filesystem().filesystem_changed.disconnect(_connect_to_filesystem)
	_thread_exit()

func _enable_plugin() -> void:
	add_autoload_singleton(BRIDGE_NAME, "Bridge.cs")

func _disable_plugin() -> void:
	ProjectSettings.clear(MAIN_SCENE_KEY)
	ProjectSettings.clear(ASSEMBLY_KEY)
	ProjectSettings.clear(LIBRARY_KEY)
	remove_autoload_singleton(BRIDGE_NAME)

# library update

var cached_unlib : Array[String] = []
var cached_lib : Dictionary[String, Library] = {}
var cached_md : Dictionary[String, String] = {}

func load_library(path) -> void:
	var dir = DirAccess.open(path)
	for f in dir.get_files():
		if f.get_extension() != "tres": 
			continue

		var file = path + "/" + f
		if cached_unlib.has(file) || cached_lib.has(file):
			continue

		var lib = load(file)
		if lib is Library:
			cached_lib[file] = lib
		else:
			cached_unlib.append(file)

	for d in dir.get_directories():
		load_library(path + "/" + d)

static func print_gray(str):
	print_rich("[color=DARK_GRAY]%s[/color]" % str)

func update_library() -> void:
	var updated = false

	var lib_path = Library.FS_PROJ.get_base_dir()
	var proj = DirAccess.open(lib_path)

	for k in cached_lib.keys():
		if !FileAccess.file_exists(k):
			cached_lib.erase(k)
			cached_md.erase(k)
			cached_unlib.erase(k)
			updated = true
			continue

		var lib = cached_lib[k]
		if !proj.file_exists(lib.get_fs_name()):
			lib.create_fs_file()
			print_gray("[Library] Added %s from: %s" % [lib.get_fs_name(),  k])
			updated = true
			cached_md[k] = FileAccess.get_md5(k)
			continue

		var md = FileAccess.get_md5(k)
		var old = cached_md[k] if cached_md.has(k) else ""
		if md != old:
			lib.create_fs_file()
			print_gray("[Library] Added %s from: %s" % [lib.get_fs_name(),  k])
			updated = true
			cached_md[k] = md

	for f in proj.get_files():
		if f.get_extension() != "fs": continue

		var file = lib_path + "/" + f
		var r = FileAccess.open(file, FileAccess.READ)
		var l = r.get_line()
		r.close()
		var path = l.substr(3)
		if !FileAccess.file_exists(path):
			print_gray("[Library] Removed %s from: %s" % [f,  path])
			proj.remove(file)
			cached_lib.erase(path)
			cached_md.erase(path)
			updated = true

	if updated:
		Library.update_props()

var should_load_lib = true
var on_update_library = Semaphore.new()

func notify_update_library():
	on_update_library.post()

func _connect_to_filesystem():
	should_load_lib = true
	notify_update_library()

var should_kill_thread = false

func _update_lib_on_thread():
	while true:
		if should_kill_thread: 
			return
	
		if should_load_lib:
			should_load_lib = false
			load_library("res://")
		
		update_library()
		on_update_library.wait()

var lib_thread : Thread

const CACHE_CFG = "res://.godot/fodot_lib_cache.cfg"

func _thread_init() -> void:
	lib_thread = Thread.new()
	var cfg = ConfigFile.new()
	if cfg.load(CACHE_CFG) == Error.OK:
		cached_unlib = cfg.get_value("cache", "unlib")
		cached_lib = cfg.get_value("cache", "lib")
		cached_md = cfg.get_value("cache", "md")
	lib_thread.start(_update_lib_on_thread)

func _thread_exit() -> void:
	should_kill_thread = true
	notify_update_library()
	lib_thread.wait_to_finish()
	var cfg = ConfigFile.new()
	cfg.set_value("cache", "unlib", cached_unlib)
	cfg.set_value("cache", "lib", cached_lib)
	cfg.set_value("cache", "md", cached_md)
	cfg.save(CACHE_CFG)

# scene access for debug

const DEBUG_FILE_PATH = "res://fodot_debug_scene"

var last_path : String
var lib_timer : float

func get_library_schedule() -> float:
	var proj = ProjectSettings.get(LIBRARY_KEY)
	if proj == null:
		return 3.0
	return proj

func _process(delta: float) -> void:
	lib_timer += delta
	var schedule_time = get_library_schedule()
	if lib_timer >= schedule_time:
		lib_timer -= schedule_time
		notify_update_library()

	var root = EditorInterface.get_edited_scene_root()
	if root == null: return

	var path = root.scene_file_path
	if path == last_path: return
	last_path = path

	var f = FileAccess.open(DEBUG_FILE_PATH, FileAccess.WRITE)
	f.store_string(path)
	f.close()