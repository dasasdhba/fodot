@tool
extends NodeSize2D
class_name DebugFill

@export var debug_color: Color = Color(0.0, 1.0, 0.0, 0.3):
	set(value):
		debug_color = value
		queue_redraw()

@export var show_in_game: bool = false

func _init() -> void:
	if Engine.is_editor_hint():
		tree_entered.connect(queue_redraw)
		size_changed.connect(queue_redraw)

func _draw() -> void:
	if not Engine.is_editor_hint() and not show_in_game:
		return
	
	draw_rect(Rect2(Vector2.ZERO, size), debug_color)