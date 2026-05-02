@tool
extends NodeSize2D
class_name GradientFill

enum GradientMode { Horizontal, Vertical }

@export var gradient: Gradient:
	set(value):
		gradient = value
		queue_redraw()

@export var sample: int = 256:
	set(value):
		sample = value
		queue_redraw()

@export var mode: GradientMode = GradientMode.Vertical:
	set(value):
		mode = value
		queue_redraw()

@export var flip: bool = false:
	set(value):
		flip = value
		queue_redraw()

var _gradient_texture: GradientTexture1D

func _init() -> void:
	tree_entered.connect(queue_redraw)
	size_changed.connect(queue_redraw)

func _draw() -> void:
	if gradient == null:
		return
	
	if _gradient_texture == null:
		_gradient_texture = GradientTexture1D.new()
	
	_gradient_texture.gradient = gradient
	_gradient_texture.width = sample
	
	var is_vertical := mode == GradientMode.Vertical
	var draw_size := size
	
	if flip:
		draw_size.x *= -1
	
	if is_vertical:
		draw_size = Vector2(draw_size.y, draw_size.x)
	
	draw_texture_rect(_gradient_texture, Rect2(Vector2.ZERO, draw_size), false, Color.WHITE, is_vertical)