namespace Fodot.Core

open System
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
        this.Object |> GodotObject.getAs<'a> this.PropName
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
        this.Object |> GodotObject.getAsArray<'a> this.PropName
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
        this.Object |> GodotObject.getAsDictionary<'a,'b> this.PropName
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
        this.Object |> GodotObject.getWithMetaAs this.PropName this.Default
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
        this.Object |> GodotObject.getWithMetaAsArray this.PropName this.Default
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
        this.Object |> GodotObject.getWithMetaAsDictionary this.PropName this.Default
    member this.Set (value : Array<'a>) =
        this.Object |> GodotObject.setWithMeta this.PropName value
    static member From<'a, 'b> (prop : string) (def : Lazy<Dictionary<'a, 'b>>) (obj : GodotObject) : GDMetaDictionary<'a, 'b> =
        {
            Object = obj
            PropName = prop
            Default = def
        }

type GDSignal<'a> =
    {
        Object : GodotObject
        SignalName : string
    }
    member private this.signalName = new StringName(this.SignalName)
    
    static member From (signal : string) (obj : GodotObject) : GDSignal<'a> =
        {
            Object = obj
            SignalName = signal
        }
    
    member this.ConnectWithFlag (call : 'a -> unit) (flags : GodotObject.ConnectFlags) =
        this.Object.Connect(
            this.signalName,
            Callable.from call,
            uint32 flags
        )
        
    member this.Connect (call : 'a -> unit) =
        this.Object.Connect(
            this.signalName,
            Callable.from call
        )
        
    member this.Disconnect (call : 'a -> unit)=
        this.Object.Disconnect(
            this.signalName,
            Callable.from call
        )
        
    member this.Emit (args : 'a) =
        this.Object.EmitSignal(this.signalName, args |> Variant.fromTuple)
        
    interface IEvent<'a> with
        member this.AddHandler handler =
            this.Connect (fun a -> handler.Invoke(null, a)) |> ignore
        
        member this.RemoveHandler handler =
            this.Disconnect (fun a -> handler.Invoke(null, a))
        
        member this.Subscribe observer =
            let handler = observer.OnNext
            let disconnect = this.Disconnect
            this.Connect handler |> ignore
            
            {
                new IDisposable with
                member this.Dispose() =
                    disconnect handler
            }
        
module GD =
    let private loadLock = obj()
    
    /// This will not init fscript, as we cannot deal with recursive case,
    /// i.e. we cannot get the subresource. 
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