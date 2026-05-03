module Fodot.Core.GodotObject

open System
open Godot
open Microsoft.FSharp.Reflection

// metadata

let setMeta (name : string) (var : 'a) (obj : GodotObject) =
    obj.SetMeta(name, var |> Variant.from)

let getMeta (name : string) (obj : GodotObject) =
    obj.GetMeta(name)

let getMetaAs<'a> (name : string) (obj : GodotObject) =
    obj.GetMeta(name) |> Variant.toType<'a>
    
let getMetaAsArray<'a> (name : string) (obj : GodotObject) =
    obj.GetMeta(name) |> Variant.toArray<'a>
    
let getMetaAsDictionary<'a, 'b> (name : string) (obj : GodotObject) =
    obj.GetMeta(name) |> Variant.toDictionary<'a, 'b>
    
let hasMeta (name : string) (obj : GodotObject) =
    obj.HasMeta(name)
    
let tryGetMeta (name : string) (obj : GodotObject) =
    if obj |> hasMeta name then
        obj.GetMeta(name) |> Some
    else
        None
    
let tryGetMetaAs<'a> (name : string) (obj : GodotObject) =
    obj |> tryGetMeta name |> Option.map (fun r -> r |> Variant.toType<'a>)
    
let tryGetMetaAsArray<'a> (name : string) (obj : GodotObject) =
    obj |> tryGetMeta name |> Option.map (fun r -> r |> Variant.toArray<'a>)

let tryGetMetaAsDictionary<'a, 'b> (name : string) (obj : GodotObject) =
    obj |> tryGetMeta name |> Option.map (fun r -> r |> Variant.toDictionary<'a, 'b>)

let removeMeta (name : string) (obj : GodotObject) =
    if obj |> hasMeta name then
        obj.RemoveMeta(name)
        true
    else
        false
    
let getMetaList (obj : GodotObject) =
    obj.GetMetaList()
    
let private getDefaultMetaWith<'a> getter (name : string) (def : Lazy<'a>) (obj : GodotObject) =
    if obj |> hasMeta name then
        obj |> getter name
    else
        obj |> setMeta name def.Value
        def.Value
    
let getMetaWithDefaultAs<'a> (name : string) (def : Lazy<'a>) (obj : GodotObject) =
    obj |> getDefaultMetaWith getMetaAs name def
        
let getMetaWithDefaultAsArray<'a> (name : string) (def : Lazy<Collections.Array<'a>>) (obj : GodotObject) =
    obj |> getDefaultMetaWith getMetaAsArray name def
        
let getMetaWithDefaultAsDictionary<'a, 'b> (name : string) (def : Lazy<Collections.Dictionary<'a, 'b>>) (obj : GodotObject) =
    obj |> getDefaultMetaWith getMetaAsDictionary name def
    
// property get set

let private createPropertyList (obj : GodotObject) =
    obj.GetPropertyList()

    |> Array.ofSeq
    |> Array.map (fun p -> p["name"] |> Variant.toType<string>)
    
let getPropertyList (obj : GodotObject) =
    let propMeta = "_fs_GodotObject_prop_list"
    obj |> getMetaWithDefaultAs propMeta (lazy createPropertyList obj)

let hasProperty (prop : string) (obj : GodotObject) =
    obj |> getPropertyList |> Array.contains prop

let get (prop : string) (obj : GodotObject) =
    if obj |> hasProperty prop |> not then
        failwith $"{obj}: Property {prop} not found."
    else
        obj.Get(prop)

let getAs<'a> (prop : string) (obj : GodotObject) =
    obj |> get prop |> Variant.toType<'a>
    
let getAsArray<'a> (prop : string) (obj : GodotObject) =
    obj |> get prop |> Variant.toArray<'a>
    
let getAsDictionary<'a, 'b> (prop : string) (obj : GodotObject) =
    obj |> get prop |> Variant.toDictionary<'a, 'b>
    
let tryGet (prop : string) (obj : GodotObject) =
    if obj |> hasProperty prop |> not then
        None
    else
        obj.Get(prop) |> Some
    
let tryGetAs<'a> (prop : string) (obj : GodotObject) =
    obj |> tryGet prop |> Option.map (fun r -> r |> Variant.toType<'a>)
    
let tryGetAsArray<'a> (prop : string) (obj : GodotObject) =
    obj |> tryGet prop |> Option.map (fun r -> r |> Variant.toArray<'a>)
    
let tryGetAsDictionary<'a, 'b> (prop : string) (obj : GodotObject) =
    obj |> tryGet prop |> Option.map (fun r -> r |> Variant.toDictionary<'a, 'b>)

let set (prop : string) (value : 'a) (obj : GodotObject) =
    if obj |> hasProperty prop |> not then
        failwith $"{obj}: Property {prop} not found."
    else
        obj.Set(prop, value |> Variant.from)

let propGetSetMeta = "_fs_GodotObject_prop_get_set"

let getFallbackMetaWith<'a>
    (getter : string -> GodotObject -> 'a)
    (getterSome : string -> GodotObject -> 'a option)
    (getterDefault : string -> Lazy<'a> -> GodotObject -> 'a)
    (prop : string) (def : Lazy<'a>) (obj : GodotObject) =
    
    let meta = $"{propGetSetMeta}_{prop}"
    if obj |> hasMeta meta then
        obj |> getter meta
    else
        match obj |> getterSome prop with
        
        | Some v -> v
        | None -> getterDefault meta def obj

let getWithMetaAs<'a> (prop : string) (def : Lazy<'a>) (obj : GodotObject) =
    obj |> getFallbackMetaWith getMetaAs tryGetAs getMetaWithDefaultAs prop def
    
let getWithMetaAsArray<'a> (prop : string) (def : Lazy<Collections.Array<'a>>) (obj : GodotObject) =
    obj |> getFallbackMetaWith getMetaAsArray tryGetAsArray getMetaWithDefaultAsArray prop def

let getWithMetaAsDictionary<'a, 'b> (prop : string) (def : Lazy<Collections.Dictionary<'a, 'b>>) (obj : GodotObject) =
    obj |> getFallbackMetaWith getMetaAsDictionary tryGetAsDictionary getMetaWithDefaultAsDictionary prop def
        
let setWithMeta (prop : string) (value : 'a) (obj : GodotObject) =
    let meta = $"{propGetSetMeta}_{prop}"
    if obj |> hasProperty prop then
        obj |> set prop value
    else
        obj |> setMeta meta value
        
// method

let hasMethod (method : string) (obj : GodotObject) =
    obj.HasMethod(method)
    
let call<'a> (method : string) (args : 'a) (obj : GodotObject) =
    obj.Call (new StringName(method), args |> Variant.fromTuple)
    
let callDeferred<'a> (method : string) (args : 'a) (obj : GodotObject) =
    obj.CallDeferred (new StringName(method), args |> Variant.fromTuple) |> ignore
    
let tryCall<'a> (method : string) (args : 'a) (obj : GodotObject) =
    if obj |> hasMethod method |> not then
        None
    else
        obj.Call (new StringName(method), args |> Variant.fromTuple) |> Some

let callAs<'a, 'b> (method : string) (args : 'a) (obj : GodotObject) =
    obj |> call<'a> method args |> Variant.toType<'b>

let callAsArray<'a, 'b> (method : string) (args : 'a) (obj : GodotObject) =
    obj |> call<'a> method args |> Variant.toArray<'b>

let callAsDictionary<'a, 'b, 'c> (method : string) (args : 'a) (obj : GodotObject) =
    obj |> call<'a> method args |> Variant.toDictionary<'b, 'c>

let tryCallAs<'a, 'b> (method : string) (args : 'a) (obj : GodotObject) =
    obj |> tryCall<'a> method args |> Option.map (fun r -> r |> Variant.toType<'b>)

let tryCallAsArray<'a, 'b> (method : string) (args : 'a) (obj : GodotObject) =
    obj |> tryCall<'a> method args |> Option.map (fun r -> r |> Variant.toArray<'b>)

let tryCallAsDictionary<'a, 'b, 'c> (method : string) (args : 'a) (obj : GodotObject) =
    obj |> tryCall<'a> method args |> Option.map (fun r -> r |> Variant.toDictionary<'b, 'c>)

let invoke (method : string) (obj : GodotObject) =
    obj |> call<unit> method ()

let invokeDeferred (method : string) (obj : GodotObject) =
    obj |> callDeferred method ()

let tryInvoke (method : string) (obj : GodotObject) =
    obj |> tryCall<unit> method ()

let invokeAs<'a> (method : string) (obj : GodotObject) =
    obj |> callAs<unit, 'a> method ()

let invokeAsArray<'a> (method : string) (obj : GodotObject) =
    obj |> callAsArray<unit, 'a> method ()

let invokeAsDictionary<'a, 'b> (method : string) (obj : GodotObject) =
    obj |> callAsDictionary<unit, 'a, 'b> method ()

let tryInvokeAs<'a> (method : string) (obj : GodotObject) =
    obj |> tryCallAs<unit, 'a> method ()

let tryInvokeAsArray<'a> (method : string) (obj : GodotObject) =
    obj |> tryCallAsArray<unit, 'a> method ()
    
let tryInvokeAsDictionary<'a, 'b> (method : string) (obj : GodotObject) =
    obj |> tryCallAsDictionary<unit, 'a, 'b> method ()

// record

/// convert godot obj's property to readonly record.
/// cannot handle typed Godot Array or Dictionary, using variant one in record instead.
let deserialize<'T when 'T : not struct> (obj: GodotObject) : 'T =
    let recordType = typeof<'T>
    
    if not (FSharpType.IsRecord recordType) then
        failwith $"{recordType.Name} is not a valid F# Record."
    
    let fields = FSharpType.GetRecordFields recordType
    
    let fieldValues =
        fields
        
        |> Array.map (fun prop ->
            let fieldName = prop.Name
            let variant = obj.Get(fieldName)
            variant.Obj |> box
        )
    
    FSharpValue.MakeRecord(recordType, fieldValues) :?> 'T