module Fodot.Core.Engine

open System
open System.Collections.Generic
open Fodot.Core.Node
open Godot

// node process data

type private ProcessData =
    {
        IdleDict : Dictionary<Guid, unit -> unit>
        IdleDeltaDict : Dictionary<Guid, float -> unit>
        PhysicsDict : Dictionary<Guid, unit -> unit>
        PhysicsDeltaDict : Dictionary<Guid, float -> unit>
    }
    static member New () = {
        IdleDict = Dictionary<Guid, unit -> unit> ()
        IdleDeltaDict = Dictionary<Guid, float -> unit> ()
        PhysicsDict = Dictionary<Guid, unit -> unit> ()
        PhysicsDeltaDict = Dictionary<Guid, float -> unit> ()
    }
    
    member this.HasIdleProcess () =
        this.IdleDeltaDict.Count > 0 || this.IdleDict.Count > 0
    
    member this.HasPhysicsProcess () =
        this.PhysicsDeltaDict.Count > 0 || this.PhysicsDict.Count > 0

    member private this.DoProcessWith (delta : Lazy<float>) p dp =
        p |> Seq.iter (fun f -> f ())
        
        if dp |> Seq.isEmpty |> not then
            let delta = delta.Value
            dp |> Seq.iter (fun f -> f delta)
    
    member this.DoIdleProcess (node: Node) =
        this.DoProcessWith (lazy node.GetProcessDeltaTime ()) this.IdleDict.Values this.IdleDeltaDict.Values
    
    member this.DoPhysicsProcess (node: Node) =
        this.DoProcessWith (lazy node.GetPhysicsProcessDeltaTime ()) this.PhysicsDict.Values this.PhysicsDeltaDict.Values
    
type private ProcessResource() =
    inherit Resource ()
    member val Data = ProcessData.New () with get

let private processInitMeta = "_fs_node_process_init"
let private processDataMeta = "_fs_node_process_data"

let private getProcessData (node: Node) =
    if node |> GodotObject.hasMeta processInitMeta |> not then
        node |> GodotObject.setMeta processInitMeta true
        node.add_TreeExited (fun () ->
            node |> GodotObject.removeMeta processDataMeta |> ignore
        )
    
    let res = node |> GodotObject.getMetaWithDefault processDataMeta (lazy new ProcessResource())
    res.Data
    
let private hasIdleProcess (node: Node) =
    (node |> getProcessData).HasIdleProcess ()
    
let private hasPhysicsProcess (node: Node) =
    (node |> getProcessData).HasPhysicsProcess ()

let addProcess (f : unit -> unit) (physics : bool) (node: Node) =
    let data = node |> getProcessData
    let dict = if physics then data.PhysicsDict else data.IdleDict
    let id = Guid.NewGuid ()
    dict.Add (id, f)
    id
    
let addDeltaProcess (f : float -> unit) (physics : bool) (node: Node) =
    let data = node |> getProcessData
    let dict = if physics then data.PhysicsDeltaDict else data.IdleDeltaDict
    let id = Guid.NewGuid ()
    dict.Add (id, f)
    id
    
let addIdleProcess (f : unit -> unit) (node: Node) =
    node |> addProcess f false
    
let addPhysicsProcess (f : unit -> unit) (node: Node) =
    node |> addProcess f true
    
let addIdleDeltaProcess (f : float -> unit) (node: Node) =
    node |> addDeltaProcess f false
   
let addPhysicsDeltaProcess (f : float -> unit) (node: Node) =
    node |> addDeltaProcess f true
    
let addDelta32Process (f : float32 -> unit) (physics : bool) (node: Node) =
    node |> addDeltaProcess (fun delta -> f (float32 delta)) physics
    
let addIdleDelta32Process (f : float32 -> unit) (node: Node) =
    node |> addDelta32Process f false
    
let addPhysicsDelta32Process (f : float32 -> unit) (node: Node) =
    node |> addDelta32Process f true

let removeProcess (id: Guid) (node: Node) =
    let data = node |> getProcessData
    data.IdleDict.Remove id || data.IdleDeltaDict.Remove id ||
    data.PhysicsDict.Remove id || data.PhysicsDeltaDict.Remove id
    
// node process logic

let mutable private cachedProcessNodes : Node list = []
let mutable private cachedPhysicsProcessNodes : Node list = []

let private updateProcessCache (tree: SceneTree) =
    let root = tree.GetCurrentScene ()
    
    cachedProcessNodes <-
        root |> getChildrenAndSelfRecWith (fun n -> n.CanProcess () && n |> hasIdleProcess)
    cachedPhysicsProcessNodes <-
        root |> getChildrenAndSelfRecWith (fun n -> n.CanProcess () && n |> hasPhysicsProcess)
        
let private doIdleProcess (node: Node) =
    let data = node |> getProcessData
    data.DoIdleProcess node

let private doPhysicsProcess (node: Node) =
    let data = node |> getProcessData
    data.DoPhysicsProcess node
    
// entry point

let private tree =
    lazy (
        let t = Engine.GetMainLoop () :?> SceneTree
        t.add_NodeAdded (fun node -> node |> FScript.init)
        t.add_TreeChanged (fun () -> updateProcessCache t)
        t.add_ProcessFrame (fun () ->
            cachedProcessNodes |> List.iter (fun n -> n |> doIdleProcess)
        )
        t.add_PhysicsFrame (fun () ->
            cachedPhysicsProcessNodes |> List.iter (fun n -> n |> doPhysicsProcess)
        )
        
        t
    )
    
let getTree () = tree.Value