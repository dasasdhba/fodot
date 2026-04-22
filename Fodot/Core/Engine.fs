module Fodot.Core.Engine

open System
open System.Collections.Generic
open Fodot.Core.GodotObject
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

let private cachedIdleUpdate = ResizeArray<Node>()
let private cachedPhysicsUpdate = ResizeArray<Node>()
let private cachedIdleRemove = ResizeArray<Node>()
let private cachedPhysicsRemove = ResizeArray<Node>()

let private updateProcessCache physics node =
    if physics then
        cachedPhysicsUpdate.Add node
    else
        cachedIdleUpdate.Add node
        
let private updateRemoveCache physics node =
    if physics then
        cachedPhysicsRemove.Add node
    else
        cachedIdleRemove.Add node

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
        node.add_TreeEntered (fun () -> node |> updateProcessCache physics)
        node.add_TreeExited (fun () -> node |> updateRemoveCache physics)
        
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
    node |> hasMeta meta

let hasIdleProcess (node: Node) =
    node |> hasProcess false
    
let hasPhysicsProcess (node: Node) =
    node |> hasProcess true

let addProcess (f : unit -> unit) (physics : bool) (node: Node) =
    let data = node |> getProcessData physics
    if node.IsInsideTree () && data.HasProcess () |> not then
        node |> updateProcessCache physics
    let dict = data.Process
    let id = Guid.NewGuid ()
    dict.Add (id, f)
    id
    
let addDeltaProcess (f : float -> unit) (physics : bool) (node: Node) =
    let data = node |> getProcessData physics
    if node.IsInsideTree () && data.HasProcess () |> not then
        node |> updateProcessCache physics
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

let private cachedProcessNodes = ResizeArray<Node>()
let private cachedPhysicsProcessNodes = ResizeArray<Node>()
let private cachedProcessData = Dictionary<Node, ProcessData>()
let private cachedPhysicsProcessData = Dictionary<Node, ProcessData>()

let private findNearestIndex (arr : ResizeArray<Node>) (node : Node) =
    if node.IsInsideTree () |> not then
        failwith $"{node}: Cannot cache a process node outside the tree."
    
    let rec search (n : Node) =
        let parent = n.GetParent ()
        if GodotObject.IsInstanceValid parent |> not then
            None
        else
            let idx = n.GetIndex true
            let r =
                [0..(idx - 1)]
                
                |> List.tryFindIndex (fun i ->
                    let c = parent.GetChild (i, true)
                    arr.Contains c
                )
                
            match r with
            | Some i -> Some (parent.GetChild (i, true))
            | None -> search parent

    if arr.Contains node then
        -1
    else
        match search node with
        | Some n -> (arr.IndexOf n) + 1
        | None -> 0

let private treeUpdateProcessCache physics=
    let queue, cache, data =
        if physics then
            cachedPhysicsUpdate, cachedPhysicsProcessNodes, cachedPhysicsProcessData
        else
            cachedIdleUpdate, cachedProcessNodes, cachedProcessData
    
    for n in queue do
        let idx = findNearestIndex cache n
        if idx >= 0 then
            cache.Insert (idx, n)
            data.Add (n, n |> getProcessData physics)
    queue.Clear ()

let private treeUpdateRemoveCache physics =
    let remove, cache, data =
        if physics then
            cachedPhysicsRemove, cachedPhysicsProcessNodes, cachedPhysicsProcessData
        else
            cachedIdleRemove, cachedProcessNodes, cachedProcessData
            
    for n in remove do
        cache.Remove n |> ignore
        data.Remove n |> ignore
    remove.Clear ()
    
// entry point

let private tree =
    lazy (
        let t = Engine.GetMainLoop () :?> SceneTree
        t.add_NodeAdded (fun node -> node |> FScript.init)
        t.add_ProcessFrame (fun () ->
            treeUpdateRemoveCache false
            treeUpdateProcessCache false
            cachedProcessNodes |> Seq.iter (fun n ->
                if n.CanProcess () then
                    let data = cachedProcessData[n]
                    data.DoProcess n
            )
        )
        t.add_PhysicsFrame (fun () ->
            treeUpdateRemoveCache true
            treeUpdateProcessCache true
            cachedPhysicsProcessNodes |> Seq.iter (fun n ->
                if n.CanProcess () then
                    let data = cachedPhysicsProcessData[n]
                    data.DoProcess n
            )
        )
        
        t
    )
    
let getTree () = tree.Value