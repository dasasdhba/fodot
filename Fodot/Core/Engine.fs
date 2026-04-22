module Fodot.Core.Engine

open System
open System.Collections.Generic
open Fodot.Core.GodotObject
open Fodot.Core.Node
open Godot

// node process data

type private ProcessData =
    {
        Physics : bool
        Process : Dictionary<Guid, unit -> unit>
        DeltaProcess : Dictionary<Guid, float -> unit>
    }
    static member New physics = {
        Physics = physics
        Process = Dictionary<Guid, unit -> unit>()
        DeltaProcess = Dictionary<Guid, float -> unit>()
    }
    
    member this.HasProcess () =
        this.Process.Count > 0 || this.DeltaProcess.Count > 0
    
    member this.DoProcess (node: Node) =
        this.Process.Values |> Seq.iter (fun f -> f ())
        
        if this.DeltaProcess.Values |> Seq.isEmpty |> not then
            let delta = if this.Physics then node.GetPhysicsProcessDeltaTime () else node.GetProcessDeltaTime ()
            this.DeltaProcess.Values |> Seq.iter (fun f -> f delta)
    
// godot needs a parameterless constructor for duplication
// so we need to separate them

type private ProcessIdleResource() =
    inherit Resource ()
    member val Data = ProcessData.New false with get
    
type private ProcessPhysicsResource() =
    inherit Resource ()
    member val Data = ProcessData.New true with get

let mutable private cachedIdleUpdate = false
let mutable private cachedPhysicsUpdate = false

let private notifyCache physics =
    if physics then
        cachedPhysicsUpdate <- true
    else
        cachedIdleUpdate <- true

let private getProcessDataMeta physics =
    if physics then
        "_fs_node_process_data_physics"
    else
        "_fs_node_process_data_idle"

let private getProcessData physics (node: Node) =
    let meta = getProcessDataMeta physics
    if node |> hasMeta meta then
        if physics then
            (node |> getMeta<ProcessPhysicsResource> meta).Data
        else
            (node |> getMeta<ProcessIdleResource> meta).Data
    else
        node.add_TreeEntered (fun () -> notifyCache physics)
        node.add_TreeExited (fun () -> notifyCache physics)
        
        let res, data =
            if physics then
                let p = new ProcessPhysicsResource()
                p :> Resource, p.Data
            else
                let p = new ProcessIdleResource()
                p :> Resource, p.Data
        
        node |> setMeta meta res
        data
    
let hasProcess physics (node: Node) =
    let meta = getProcessDataMeta physics
    if node |> hasMeta meta |> not then
        false
    else
        let data = node |> getProcessData physics
        data.HasProcess ()

let hasIdleProcess (node: Node) =
    node |> hasProcess false
    
let hasPhysicsProcess (node: Node) =
    node |> hasProcess true

let addProcess (f : unit -> unit) (physics : bool) (node: Node) =
    let data = node |> getProcessData physics
    if node.IsInsideTree () && data.HasProcess () |> not then
        notifyCache physics
    let dict = data.Process
    let id = Guid.NewGuid ()
    dict.Add (id, f)
    id
    
let addDeltaProcess (f : float -> unit) (physics : bool) (node: Node) =
    let data = node |> getProcessData physics
    if node.IsInsideTree () && data.HasProcess () |> not then
        notifyCache physics
    let dict = data.DeltaProcess
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

let private removeProcessWith physics (id: Guid) (node: Node) =
    if node |> hasProcess physics |> not then
        false
    else
        let data = node |> getProcessData physics
        data.Process.Remove id || data.DeltaProcess.Remove id

let removeIdleProcess (id: Guid) (node: Node) =
    node |> removeProcessWith false id
    
let removePhysicsProcess (id: Guid) (node: Node) =
    node |> removeProcessWith true id

let removeProcess (id: Guid) (node: Node) =
    node |> removeIdleProcess id || node |> removePhysicsProcess id

// node process logic

let mutable private cachedProcessNodes : Node list = []
let mutable private cachedPhysicsProcessNodes : Node list = []

let private updateIdleProcessCache (tree: SceneTree) =
    let root = tree.GetCurrentScene ()
    
    cachedProcessNodes <-
        root |> getChildrenAndSelfRecWith (fun n -> n.CanProcess () && n |> hasIdleProcess)
    
let private updatePhysicsProcessCache (tree: SceneTree) =
    let root = tree.GetCurrentScene ()
    
    cachedPhysicsProcessNodes <-
        root |> getChildrenAndSelfRecWith (fun n -> n.CanProcess () && n |> hasPhysicsProcess)
        
let private doProcess physics (node: Node) =
    let data = node |> getProcessData physics
    data.DoProcess node
    
// entry point

let private tree =
    lazy (
        let t = Engine.GetMainLoop () :?> SceneTree
        t.add_NodeAdded (fun node -> node |> FScript.init)
        t.add_ProcessFrame (fun () ->
            if cachedIdleUpdate then
                cachedIdleUpdate <- false
                updateIdleProcessCache t
            
            cachedProcessNodes |> List.iter (fun n -> n |> doProcess false)
        )
        t.add_PhysicsFrame (fun () ->
            if cachedPhysicsUpdate then
                cachedPhysicsUpdate <- false
                updatePhysicsProcessCache t
            
            cachedPhysicsProcessNodes |> List.iter (fun n -> n |> doProcess true)
        )
        
        t
    )
    
let getTree () = tree.Value