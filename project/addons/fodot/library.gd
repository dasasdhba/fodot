@tool
extends Resource
class_name Library

@export_category("Library")
@export var lib_name : String = ""
@export var lib : Dictionary[String, Resource] = {}

func get_lib_name():
	if lib_name == "":
		return resource_path.get_file().get_basename().to_pascal_case()
	return lib_name

func get_fs_content():
	var library = get_lib_name()
	var id = ResourceLoader.get_resource_uid(resource_path)
	var id_text = ResourceUID.id_to_text(id)
	var result = ""
	result += "// %s\n" % resource_path
	result += "module " + library + " =\n"
	result += "    let private _back_lib = GDLib(\"%s\")\n\n" % id_text
	var all = ""
	for k in lib.keys():
		var v = lib[k]
		var type = v.get_class()
		var kname = k.to_camel_case()
		if all != "": all += "; "
		all += kname
		result += "    let %s = _back_lib.Get<%s>(\"%s\")\n" % [kname, type, k]
	result += "\n    let lib = _back_lib.Lib\n"
	result += "    let all : Resource list = [%s]" % all
	return result
