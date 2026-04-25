extends Node

@export_category("SceneManager")
@export var viewports : Array[NodePath]

func _get_fscripts() :
	return [ "scene_manager" ]
