extends Node

@export_category("CutsceneProvider")
@export_custom(PROPERTY_HINT_NONE, "") var in_scene : PackedScene
@export_custom(PROPERTY_HINT_NONE, "") var out_scene : PackedScene
@export_custom(PROPERTY_HINT_NONE, "") var reuse_in_if_out_is_null : bool = true

func _get_fscripts():
	return ["cutscene_provider"]
