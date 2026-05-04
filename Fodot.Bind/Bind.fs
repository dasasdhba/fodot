namespace Fodot.Bind

open Fodot.Core
open Godot

type Example(obj : Node) =

    let _back_prop_my_int = GDProp<int64>.From("my_int") obj
    let _back_prop_my_float = GDProp<float>.From("my_float") obj

    let _back_prop_my_str = GDProp<string>.From("my_str") obj
    let _back_prop_my_bool = GDProp<bool>.From("my_bool") obj
    let _back_prop_my_vector = GDProp<Vector2>.From("my_vector") obj

    let _back_prop_my_scene = GDProp<PackedScene>.From("my_scene") obj
    let _back_prop_my_array = GDPropArray<int64>.From("my_array") obj
    let _back_prop_my_dict = GDPropDictionary<string, int64>.From("my_dict") obj

    let _back_signal_my_signal = GDSignal<unit>.From("my_signal") obj
    let _back_signal_my_arg_signal = GDSignal<int64>.From("my_arg_signal") obj


    member this.MyInt
        with get () = _back_prop_my_int.Get()
        and set v = _back_prop_my_int.Set v
    member this.MyFloat
        with get () = _back_prop_my_float.Get()
        and set v = _back_prop_my_float.Set v

    member this.MyStr
        with get () = _back_prop_my_str.Get()
        and set v = _back_prop_my_str.Set v
    member this.MyBool
        with get () = _back_prop_my_bool.Get()
        and set v = _back_prop_my_bool.Set v
    member this.MyVector
        with get () = _back_prop_my_vector.Get()
        and set v = _back_prop_my_vector.Set v

    member this.MyScene
        with get () = _back_prop_my_scene.Get()
        and set v = _back_prop_my_scene.Set v
    member this.MyArray
        with get () = _back_prop_my_array.Get()
        and set v = _back_prop_my_array.Set v
    member this.MyDict
        with get () = _back_prop_my_dict.Get()
        and set v = _back_prop_my_dict.Set v

    member val MySignal = _back_signal_my_signal with get
    member val MyArgSignal = _back_signal_my_arg_signal with get