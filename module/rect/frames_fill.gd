@tool
extends NodeSize2D
class_name FramesFill

@export var frames: SpriteFrames:
	set(value):
		frames = value
		if frames != null and frames.get_animation_names().size() > 0:
			animation = frames.get_animation_names()[0]
		_reset()
		queue_redraw()
		notify_property_list_changed()

@export var animation: StringName = &"Default":
	set(value):
		animation = value
		_reset()
		queue_redraw()

@export var speed_scale: float = 1.0
@export var paused: bool = false
@export var flip_h: bool:
	set(value):
		flip_h = value
		queue_redraw()
@export var flip_v: bool:
	set(value):
		flip_v = value
		queue_redraw()

var _frame: int = 0
var _progress: float = 0.0
var _frame_speed: float = 1.0

func _init() -> void:
	tree_entered.connect(queue_redraw)
	size_changed.connect(queue_redraw)

func _reset() -> void:
	_frame = 0
	_progress = 0.0

func _physics_process(delta: float) -> void:
	_animate(delta)

func _animate(delta: float) -> void:
	if paused:
		return
	if frames == null:
		return
	if not frames.has_animation(animation):
		return
	
	var frame_count: int = frames.get_frame_count(animation)
	var last_frame: int = frame_count - 1
	var anim_speed: float = frames.get_animation_speed(animation) * speed_scale
	
	if anim_speed == 0.0:
		return
	
	if _frame < 0 or _frame >= frame_count:
		_frame = 0
	
	_frame_speed = 1.0 / frames.get_frame_duration(animation, _frame)
	
	var speed: float = anim_speed * _frame_speed
	var abs_speed: float = abs(speed)
	var last: int = _frame
	var i: int = 0
	
	while delta > 0.0 and i <= frame_count:
		if speed > 0.0:
			if _progress >= 1.0:
				if _frame >= last_frame:
					_frame = 0
				else:
					_frame += 1
				_frame_speed = 1.0 / frames.get_frame_duration(animation, _frame)
				_progress = 0.0
			
			var to_process: float = min((1.0 - _progress) / abs_speed, delta)
			_progress += to_process * abs_speed
			delta -= to_process
		else:
			if _progress <= 0.0:
				if _frame <= 0:
					_frame = last_frame
				else:
					_frame -= 1
				_frame_speed = 1.0 / frames.get_frame_duration(animation, _frame)
				_progress = 1.0
			
			var to_process: float = min(_progress / abs_speed, delta)
			_progress -= to_process * abs_speed
			delta -= to_process
		
		i += 1
	
	if _frame != last:
		queue_redraw()

func _draw() -> void:
	if frames == null:
		return
	if not frames.has_animation(animation):
		return
	
	var texture: Texture2D = frames.get_frame_texture(animation, _frame)
	var draw_size: Vector2 = size
	
	if flip_h:
		draw_size.x *= -1
	if flip_v:
		draw_size.y *= -1
	
	_draw_texture_rect_tiled(texture, Rect2(Vector2.ZERO, draw_size))

func _draw_texture_rect_tiled(texture: Texture2D, rect: Rect2, modulate_color: Color = Color.WHITE) -> void:
	var src_rect := Rect2(Vector2.ZERO, texture.get_size())
	_draw_texture_rect_region_tiled(texture, rect, src_rect, modulate_color)

func _draw_texture_rect_region_tiled(texture: Texture2D, rect: Rect2, src_rect: Rect2, modulate_color: Color = Color.WHITE) -> void:
	var tex_flip_h := rect.size.x * src_rect.size.x < 0
	var tex_flip_v := rect.size.y * src_rect.size.y < 0
	rect = Rect2(rect.position, rect.size.abs())
	src_rect = Rect2(src_rect.position, src_rect.size.abs())
	
	var rx := rect.size.x
	var ry := rect.size.y
	var ux := src_rect.size.x
	var uy := src_rect.size.y
	if ux <= 0 or uy <= 0:
		return
	
	var px := 0.0
	while px < rx:
		var py := 0.0
		while py < ry:
			var w : float = min(ux, rx - px)
			var h : float = min(uy, ry - py)
			var x := ux - w if tex_flip_h else 0.0
			var y := uy - h if tex_flip_v else 0.0
			var s_rect := Rect2(x, y, w, h)
			if tex_flip_h:
				w *= -1
			if tex_flip_v:
				h *= -1
			var r_rect := Rect2(rect.position + Vector2(px, py), Vector2(w, h))
			draw_texture_rect_region(texture, r_rect, s_rect, modulate_color)
			py += uy
		px += ux

func _validate_property(property: Dictionary) -> void:
	if property["name"] == "animation":
		property["hint"] = PROPERTY_HINT_ENUM
		if frames == null:
			property["hint_string"] = "Default"
		else:
			var names: Array = frames.get_animation_names()
			property["hint_string"] = ",".join(names)