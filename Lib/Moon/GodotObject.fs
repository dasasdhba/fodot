module Lib.Moon.GodotObject

open System
open Godot

// metadata

let setMeta (name : string) (var : Variant) (obj : GodotObject) =
    obj.SetMeta(name, var)
    
let getMeta<'a> (name : string) (obj : GodotObject) =
    obj.GetMeta(name) |> Variant.toType<'a>
    
let hasMeta (name : string) (obj : GodotObject) =
    obj.HasMeta(name)
    
let getSomeMeta<'a> (name : string) (obj : GodotObject) =
    if obj |> hasMeta name then
        obj.GetMeta(name) |> Variant.toSomeType<'a>
    else
        None
    
let removeMeta (name : string) (obj : GodotObject) =
    if obj |> hasMeta name then
        obj.RemoveMeta(name)
        true
    else
        false
    
let getMetaList (obj : GodotObject) =
    obj.GetMetaList()
    
let getMetaWithDefault<[<MustBeVariant>] 'a> (name : string) (def : Lazy<'a>) (obj : GodotObject) =
    if obj |> hasMeta name then
        obj |> getMeta name
    else
        let value = def.Value
        let ref = &value
        obj |> setMeta name (Variant.From &ref)
        def.Value