@tool
extends NodeSize2D
class_name ColorFill

@export var color: Color = Color.BLACK:
	set(value):
		color = value
		queue_redraw()

func _init() -> void:
	tree_entered.connect(queue_redraw)
	size_changed.connect(queue_redraw)

func _draw() -> void:
	draw_rect(Rect2(Vector2.ZERO, size), color)