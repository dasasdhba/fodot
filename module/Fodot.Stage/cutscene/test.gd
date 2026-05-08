extends Node

@export var delay : float = 0.5
@export_enum("in", "out", "in out") var mode : int = 0

func _ready():
	for c in get_children():
		if c.has_method("fade_in") && c.has_method("fade_out"):
			test_with(c)
			break

func test_with(cutscene):
	if mode == 1:
		if cutscene.has_method("fade_out_init"):
			cutscene.fade_out_init()
	else:
		if cutscene.has_method("fade_in_init"):
			cutscene.fade_in_init()

	await get_tree().create_timer(delay).timeout

	if mode == 0:
		cutscene.fade_in()
		await cutscene.fade_in_finished
		print("Fade in test end.")
	elif mode == 1:
		cutscene.fade_out()
		await cutscene.fade_out_finished
		print("Fade out test end.")
	else:
		cutscene.fade_in()
		await cutscene.fade_in_finished
		cutscene.fade_out()
		await cutscene.fade_out_finished
		print("Fade in out test end.")