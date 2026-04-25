module Fodot.Core.GodotObject

open System
open Godot
open Microsoft.FSharp.Reflection

// metadata

let setMeta (name : string) (var : 'a) (obj : GodotObject) =
    obj.SetMeta(name, var |> Variant.from)
    
let getMeta<'a> (name : string) (obj : GodotObject) =
    obj.GetMeta(name) |> Variant.toType<'a>
    
let getMetaArray<'a> (name : string) (obj : GodotObject) =
    obj.GetMeta(name) |> Variant.toArray<'a>
    
let getMetaDictionary<'a, 'b> (name : string) (obj : GodotObject) =
    obj.GetMeta(name) |> Variant.toDictionary<'a, 'b>
    
let hasMeta (name : string) (obj : GodotObject) =
    obj.HasMeta(name)
    
let private tryGetMetaWith converter (name : string) (obj : GodotObject) =
    if obj |> hasMeta name then
        obj.GetMeta(name) |> converter
    else
        None
    
let tryGetMeta<'a> (name : string) (obj : GodotObject) =
    obj |> tryGetMetaWith Variant.toSome<'a> name
    
let tryGetMetaArray<'a> (name : string) (obj : GodotObject) =
    obj |> tryGetMetaWith Variant.toSomeArray<'a> name

let tryGetMetaDictionary<'a, 'b> (name : string) (obj : GodotObject) =
    obj |> tryGetMetaWith Variant.toSomeDictionary<'a, 'b> name

let removeMeta (name : string) (obj : GodotObject) =
    if obj |> hasMeta name then
        obj.RemoveMeta(name)
        true
    else
        false
    
let getMetaList (obj : GodotObject) =
    obj.GetMetaList()
    
let private getDefaultMetaWith getter (name : string) (def : Lazy<'a>) (obj : GodotObject) =
    if obj |> hasMeta name then
        obj |> getter name
    else
        obj |> setMeta name def.Value
        def.Value
    
let getMetaWithDefault<'a> (name : string) (def : Lazy<'a>) (obj : GodotObject) =
    obj |> getDefaultMetaWith getMeta name def
        
let getMetaArrayWithDefault<'a> (name : string) (def : Lazy<Collections.Array<'a>>) (obj : GodotObject) =
    obj |> getDefaultMetaWith getMetaArray name def
        
let getMetaDictionaryWithDefault<'a, 'b> (name : string) (def : Lazy<Collections.Dictionary<'a, 'b>>) (obj : GodotObject) =
    obj |> getDefaultMetaWith getMetaDictionary name def
    
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

let getWith converter (prop : string) (obj : GodotObject) =
    if obj |> hasProperty prop |> not then
        failwith $"{obj}: Property {prop} not found."
    else
        obj.Get(prop) |> converter

let get<'a> (prop : string) (obj : GodotObject) =
    obj |> getWith Variant.toType<'a> prop
    
let getArray<'a> (prop : string) (obj : GodotObject) =
    obj |> getWith Variant.toArray<'a> prop
    
let getDictionary<'a, 'b> (prop : string) (obj : GodotObject) =
    obj |> getWith Variant.toDictionary<'a, 'b> prop
    
let private tryGetWith converter (prop : string) (obj : GodotObject) =
    if obj |> hasProperty prop |> not then
        None
    else
        obj.Get(prop) |> converter
    
let tryGet<'a> (prop : string) (obj : GodotObject) =
    obj |> tryGetWith Variant.toSome<'a> prop
    
let tryGetArray<'a> (prop : string) (obj : GodotObject) =
    obj |> tryGetWith Variant.toSomeArray<'a> prop
    
let tryGetDictionary<'a, 'b> (prop : string) (obj : GodotObject) =
    obj |> tryGetWith Variant.toSomeDictionary<'a, 'b> prop

let set (prop : string) (value : 'a) (obj : GodotObject) =
    if obj |> hasProperty prop |> not then
        failwith $"{obj}: Property {prop} not found."
    else
        obj.Set(prop, value |> Variant.from)

let propGetSetMeta = "_fs_GodotObject_prop_get_set"

let getFallbackMetaWith getter getterSome getterDefault (prop : string) (def : Lazy<'a>) (obj : GodotObject) =
    let meta = $"{propGetSetMeta}_{prop}"
    if obj |> hasMeta meta then
        obj |> getter meta
    else
        match obj |> getterSome prop with
        
        | Some v -> v
        | None -> getterDefault meta def obj

let getWithMeta (prop : string) (def : Lazy<'a>) (obj : GodotObject) =
    obj |> getFallbackMetaWith getMeta tryGet getMetaWithDefault prop def
    
let getArrayWithMeta (prop : string) (def : Lazy<Collections.Array<'a>>) (obj : GodotObject) =
    obj |> getFallbackMetaWith getMetaArray tryGetArray getMetaArrayWithDefault prop def

let getDictionaryWithMeta (prop : string) (def : Lazy<Collections.Dictionary<'a, 'b>>) (obj : GodotObject) =
    obj |> getFallbackMetaWith getMetaDictionary tryGetDictionary getMetaDictionaryWithDefault prop def
        
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
    
let callDeferred (method : string) (args : Variant[]) (obj : GodotObject) =
    obj.CallDeferred (new StringName(method), args) |> ignore
    
let tryCall<'a> (method : string) (args : Variant[]) (obj : GodotObject) =
    if obj |> hasMethod method |> not then
        None
    else
        obj.Call (new StringName(method), args) |> Variant.toSome<'a>
        
// signal

let toSignal (name : string) (obj : GodotObject) =
    GodotTask.GDTask.FromSignal(obj, name)

let toSignalWith ct (name : string) (obj : GodotObject) =
    GodotTask.GDTask.FromSignal(obj, name, ct)

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