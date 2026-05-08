@tool
extends Node2D
class_name NodeSize2D

signal size_changed

@export var size: Vector2 = Vector2(32, 32):
	set(value):
		if size != value:
			size = value
			size_changed.emit()

func _editor_get_state(state: Dictionary) -> void:
	state["size"] = size;

func _editor_set_state(state: Dictionary) -> void:
	size = state["size"]

func _editor_use_rect() -> bool:
	return true

func _editor_get_rect() -> Rect2:
	return Rect2(Vector2.ZERO, size).abs()

func _editor_set_rect(rect: Rect2) -> void:
	position += transform.basis_xform(rect.position).snappedf(1)
	size = rect.size.abs().snappedf(1)

func _editor_use_pivot() -> bool:
	return true

func _editor_get_pivot() -> Vector2:
	return Vector2.ZERO

func _editor_set_pivot(pivot: Vector2) -> void:
	position = transform * pivot