@tool
extends NodeSize2D
class_name TextureFill

@export var texture: Texture2D:
	set(value):
		texture = value
		queue_redraw()

@export var flip_h: bool:
	set(value):
		flip_h = value
		queue_redraw()

@export var flip_v: bool:
	set(value):
		flip_v = value
		queue_redraw()

func _init() -> void:
	tree_entered.connect(queue_redraw)
	size_changed.connect(queue_redraw)

func _draw() -> void:
	if texture == null:
		return
	
	var draw_size := size
	if flip_h:
		draw_size.x *= -1
	if flip_v:
		draw_size.y *= -1
	
	draw_texture_rect(texture, Rect2(Vector2.ZERO, draw_size), true)