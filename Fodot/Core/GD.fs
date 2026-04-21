namespace Fodot.Core

open Godot
open Godot.Collections

/// A wrapper for GodotObject's property.
/// May fail if the property does not exist.
type GDProp<'a> =
    {
        Object : GodotObject
        PropName : string
    }
    
    member this.Get () =
        this.Object |> GodotObject.get this.PropName
    member this.Set (value : 'a) =
        this.Object |> GodotObject.set this.PropName value
    static member From<'a> (prop : string) (obj : GodotObject) : GDProp<'a> =
        {
            Object = obj
            PropName = prop
        }
        
/// A wrapper for GodotObject's array property.
/// May fail if the property does not exist.
type GDPropArray<'a> =
    {
        Object : GodotObject
        PropName : string
    }
    
    member this.Get () =
        this.Object |> GodotObject.getArray<'a> this.PropName
    member this.Set (value : Array<'a>) =
        this.Object |> GodotObject.set this.PropName value
    static member From<'a> (prop : string) (obj : GodotObject) : GDPropArray<'a> =
        {
            Object = obj
            PropName = prop
        }

/// A wrapper for GodotObject's dictionary property.
/// May fail if the property does not exist.   
type GDPropDictionary<'a, 'b> =
    {
        Object : GodotObject
        PropName : string
    }
    
    member this.Get () =
        this.Object |> GodotObject.getDictionary<'a,'b> this.PropName
    member this.Set (value : Dictionary<'a,'b>) =
        this.Object |> GodotObject.set this.PropName value
    static member From<'a, 'b> (prop : string) (obj : GodotObject) : GDPropDictionary<'a, 'b> =
        {
            Object = obj
            PropName = prop
        }

/// A wrapper for GodotObject's property.
/// Will fall back to metadata if the property does not exist.
type GDMeta<'a> =
    {
        Object : GodotObject
        PropName : string
        Default : Lazy<'a>
    }
    
    member this.Get () =
        this.Object |> GodotObject.getWithMeta this.PropName this.Default
    member this.Set (value : 'a) =
        this.Object |> GodotObject.setWithMeta this.PropName value
    static member From<'a> (prop : string) (def : Lazy<'a>) (obj : GodotObject) : GDMeta<'a> =
        {
            Object = obj
            PropName = prop
            Default = def
        }

/// A wrapper for GodotObject's array property.
/// Will fall back to metadata if the property does not exist.
type GDMetaArray<'a> =
    {
        Object : GodotObject
        PropName : string
        Default : Lazy<Array<'a>>
    }
    
    member this.Get () =
        this.Object |> GodotObject.getArrayWithMeta this.PropName this.Default
    member this.Set (value : Array<'a>) =
        this.Object |> GodotObject.setWithMeta this.PropName value
    static member From<'a> (prop : string) (def : Lazy<Array<'a>>) (obj : GodotObject) : GDMetaArray<'a> =
        {
            Object = obj
            PropName = prop
            Default = def
        }

/// A wrapper for GodotObject's dictionary property.
/// Will fall back to metadata if the property does not exist.
type GDMetaDictionary<'a, 'b> =
    {
        Object : GodotObject
        PropName : string
        Default : Lazy<Dictionary<'a, 'b>>
    }
    
    member this.Get () =
        this.Object |> GodotObject.getDictionaryWithMeta this.PropName this.Default
    member this.Set (value : Array<'a>) =
        this.Object |> GodotObject.setWithMeta this.PropName value
    static member From<'a, 'b> (prop : string) (def : Lazy<Dictionary<'a, 'b>>) (obj : GodotObject) : GDMetaDictionary<'a, 'b> =
        {
            Object = obj
            PropName = prop
            Default = def
        }
        
module GD =
    
    let private loadLock = obj()
    
    let load (path : string) =
        lock loadLock (fun () ->
            GD.Load path
        )
        
    let loadAs<'a when 'a :> Resource> (path : string) =
        match load path with
        
        | :? 'a as obj -> obj
        | _ -> failwith $"Failed loading {path} as {typeof<'a>}"
        
    let loadSome<'a when 'a :> Resource> (path : string) =
        try
            loadAs<'a> path |> Some
        with
        | _ -> None