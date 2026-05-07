namespace Fodot.Stage.Bind

open Fodot.Core
open Godot

type CutsceneProvider(obj : Node) =

    let _back_prop_in_scene = GDNullProp<PackedScene>.From("in_scene") obj
    let _back_prop_out_scene = GDNullProp<PackedScene>.From("out_scene") obj
    let _back_prop_reuse_in_if_out_is_null = GDProp<bool>.From("reuse_in_if_out_is_null") obj

    member this.InScene
        with get () = _back_prop_in_scene.Get()
        and set v = _back_prop_in_scene.Set v
    member this.OutScene
        with get () = _back_prop_out_scene.Get()
        and set v = _back_prop_out_scene.Set v
    member this.ReuseInIfOutIsNull
        with get () = _back_prop_reuse_in_if_out_is_null.Get()
        and set v = _back_prop_reuse_in_if_out_is_null.Set v
