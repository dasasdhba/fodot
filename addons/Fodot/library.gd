@tool
extends Resource
class_name Library

@export_category("Library")
@export var lib_name : String = ""
@export var lib : Dictionary[String, Variant] = {}

func get_lib_name():
	if lib_name == "":
		return resource_path.get_file().get_basename().to_pascal_case()
	return lib_name

static func get_type_name(t):
	match typeof(t):
		TYPE_BOOL:
			return "bool"
		TYPE_INT:
			return "int64"
		TYPE_FLOAT:
			return "float"
		TYPE_STRING:
			return "string"
		TYPE_VECTOR2:
			return "Vector2"
		TYPE_VECTOR2I:
			return "Vector2i"
		TYPE_RECT2:
			return "Rect2"
		TYPE_RECT2I:
			return "Rect2i"
		TYPE_VECTOR3:
			return "Vector3"
		TYPE_VECTOR3I:
			return "Vector3i"
		TYPE_TRANSFORM2D:
			return "Transform2D"
		TYPE_VECTOR4:
			return "Vector4"
		TYPE_VECTOR4I:
			return "Vector4i"
		TYPE_PLANE:
			return "Plane"
		TYPE_QUATERNION:
			return "Quaternion"
		TYPE_AABB:
			return "AABB"
		TYPE_BASIS:
			return "Basis"
		TYPE_TRANSFORM3D:
			return "Transform3D"
		TYPE_PROJECTION:
			return "Projection"
		TYPE_COLOR:
			return "Color"
		TYPE_STRING_NAME:
			return "StringName"
		TYPE_NODE_PATH:
			return "NodePath"
		TYPE_RID:
			return "Rid"
		TYPE_OBJECT:
			return t.get_class()
		TYPE_CALLABLE:
			return "Callable"
		TYPE_SIGNAL:
			return "Signal"
		TYPE_DICTIONARY:
			return "Collections.Dictionary"
		TYPE_ARRAY:
			return "Collections.Array"
		TYPE_PACKED_BYTE_ARRAY:
			return "byte[]"
		TYPE_PACKED_INT32_ARRAY:
			return "int[]"
		TYPE_PACKED_INT64_ARRAY:
			return "int64[]"
		TYPE_PACKED_FLOAT32_ARRAY:
			return "float32[]"
		TYPE_PACKED_FLOAT64_ARRAY:
			return "float[]"
		TYPE_PACKED_STRING_ARRAY:
			return "string[]"
		TYPE_PACKED_VECTOR2_ARRAY:
			return "Vector2[]"
		TYPE_PACKED_VECTOR3_ARRAY:
			return "Vector3[]"
		TYPE_PACKED_COLOR_ARRAY:
			return "Color[]"
		TYPE_PACKED_VECTOR4_ARRAY:
			return "Vector4[]"
		TYPE_NIL:
			return "null"

func rip_export_props():
	var result = ""
	for pd in get_property_list():
		var usage = pd["usage"]
		if usage & PROPERTY_USAGE_SCRIPT_VARIABLE == 0:
			continue
		
		var name = pd["name"]
		if name == "lib" or name == "lib_name":
			continue
		
		var sname = "_%s_str_name" % name
		result += "    let private %s = new StringName \"%s\"\n" % [sname, name]

		var kname = name.to_camel_case()
		var typ = pd["type"]
		var tname = get_type_name(typ)
	
		result += "    let %s = _back_lib.Resource |> GodotObject.getAs<%s> %s\n" % [kname, tname, sname]
	return result

func get_fs_content():
	var library = get_lib_name()
	var id = ResourceLoader.get_resource_uid(resource_path)
	var id_text = ResourceUID.id_to_text(id)
	var result = ""
	result += "// %s\n" % resource_path
	result += "module " + library + " =\n"
	result += "    let private _back_lib = GDLib(\"%s\")\n\n" % id_text
	for k in lib.keys():
		var v = lib[k]
		var type = get_type_name(v)
		if type == "null": continue
		var kname = k.to_camel_case()
		result += "    let %s = _back_lib.Get<%s>(\"%s\")\n" % [kname, type, k]
	result += "\n" + rip_export_props()
	result += "\n    let lib = _back_lib.Lib\n"
	return result
