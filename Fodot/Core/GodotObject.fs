module Fodot.Core.GodotObject

open System
open Godot

// metadata

let setMeta (name : string) (var : 'a) (obj : GodotObject) =
    obj.SetMeta(name, var |> Variant.from)
    
let getMeta<'a> (name : string) (obj : GodotObject) =
    obj.GetMeta(name) |> Variant.toType<'a>
    
let hasMeta (name : string) (obj : GodotObject) =
    obj.HasMeta(name)
    
let getSomeMeta<'a> (name : string) (obj : GodotObject) =
    if obj |> hasMeta name then
        obj.GetMeta(name) |> Variant.toSome<'a>
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
    
let getMetaWithDefault (name : string) (def : Lazy<'a>) (obj : GodotObject) =
    if obj |> hasMeta name then
        obj |> getMeta name
    else
        obj |> setMeta name def.Value
        def.Value
    
// property get set

let private createPropertyList (obj : GodotObject) =
    obj.GetPropertyList()

    |> Array.ofSeq
    |> Array.map (fun p -> p["name"] |> Variant.toType<string>)
    
let getPropertyList (obj : GodotObject) =
    let propMeta = "_fs_GodotObject_prop_list"
    obj |> getMetaWithDefault propMeta (lazy createPropertyList obj)

let hasProperty (prop : string) (obj : GodotObject) =
    obj |> getPropertyList |> Array.contains prop

let get<'a> (prop : string) (obj : GodotObject) =
    if obj |> hasProperty prop |> not then
        raise (Exception($"{obj}: Property {prop} not found."))
    else
        obj.Get(prop) |> Variant.toType<'a>
    
let getSome<'a> (prop : string) (obj : GodotObject) =
    if obj |> hasProperty prop |> not then
        None
    else
        obj.Get(prop) |> Variant.toSome<'a>

let set (prop : string) (value : 'a) (obj : GodotObject) =
    if obj |> hasProperty prop |> not then
        raise (Exception($"{obj}: Property {prop} not found."))
    else
        obj.Set(prop, value |> Variant.from)

let propGetSetMeta = "_fs_GodotObject_prop_get_set"

let getWithMeta (prop : string) (def : Lazy<'a>) (obj : GodotObject) =
    let meta = $"{propGetSetMeta}_{prop}"
    if obj |> hasMeta meta then
        obj |> getMeta meta
    else
        match obj |> getSome prop with
        
        | Some v -> v
        | None -> getMetaWithDefault meta def obj
        
let setWithMeta (prop : string) (value : 'a) (obj : GodotObject) =
    let meta = $"{propGetSetMeta}_{prop}"
    if obj |> hasProperty prop then
        obj |> set prop value
    else
        obj |> setMeta meta value
        
// method

let hasMethod (method : string) (obj : GodotObject) =
    obj.HasMethod(method)
    
let call<'a> (method : string) (args : Variant[]) (obj : GodotObject) =
    obj.Call (new StringName(method), args) |> Variant.toType<'a>
    
let callSome<'a> (method : string) (args : Variant[]) (obj : GodotObject) =
    if obj |> hasMethod method |> not then
        None
    else
        obj.Call (new StringName(method), args) |> Variant.toSome<'a>