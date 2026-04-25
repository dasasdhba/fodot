module Fodot.Core.Engine

open System
open System.Collections.Concurrent
open System.Collections.Generic
open Fodot.Core.GodotObject
open Godot

// node process data

type ProcessFunc<'a> =
    | Unit of (unit -> 'a)
    | Delta of (float -> 'a)
    | Delta32 of (float32 -> 'a)
    
    member this.Invoke (delta : float) =
        match this with
        | Unit f -> f ()
        | Delta f -> f delta
        | Delta32 f -> f (float32 delta)

type ProcessUnit = ProcessFunc<unit>
    
type private ProcessData() =
    inherit Resource ()
    member val Process = ConcurrentDictionary<Guid, ProcessUnit>() with get
    member this.HasProcess () =
        this.Process.Count > 0
    member this.DoProcess delta =
        this.Process.Values |> Seq.iter (fun f -> f.Invoke delta)

let private cachedIdleUpdate = ConcurrentBag<Node>()
let private cachedPhysicsUpdate = ConcurrentBag<Node>()
let private cachedIdleRemove = ConcurrentBag<Node>()
let private cachedPhysicsRemove = ConcurrentBag<Node>()

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
        node |> getMeta<ProcessData> meta
    else
        node.add_TreeEntered (fun () -> node |> updateProcessCache physics)
        node.add_TreeExited (fun () -> node |> updateRemoveCache physics)
        
        let res = new ProcessData()
        node |> setMeta meta res
        res
    
let hasProcess physics (node: Node) =
    let meta = getProcessDataMeta physics
    node |> hasMeta meta

let hasIdleProcess (node: Node) =
    node |> hasProcess false
    
let hasPhysicsProcess (node: Node) =
    node |> hasProcess true

let addProcessType (f : ProcessUnit) (physics : bool) (node: Node) =
    let data = node |> getProcessData physics
    if node.IsInsideTree () && data.HasProcess () |> not then
        node |> updateProcessCache physics
    let dict = data.Process
    let id = Guid.NewGuid ()
    dict.AddOrUpdate(id, (fun _ -> f), (fun _ __ -> f)) |> ignore
    id

let addProcess (f : unit -> unit) (physics : bool) (node: Node) =
    node |> addProcessType (Unit f) physics
    
let addDeltaProcess (f : float -> unit) (physics : bool) (node: Node) =
    node |> addProcessType (Delta f) physics

let addDelta32Process (f : float32 -> unit) (physics : bool) (node: Node) =
    node |> addProcessType (Delta32 f) physics

let addIdleProcess (f : unit -> unit) (node: Node) =
    node |> addProcess f false
    
let addPhysicsProcess (f : unit -> unit) (node: Node) =
    node |> addProcess f true
    
let addIdleDeltaProcess (f : float -> unit) (node: Node) =
    node |> addDeltaProcess f false
   
let addPhysicsDeltaProcess (f : float -> unit) (node: Node) =
    node |> addDeltaProcess f true
    
let addIdleDelta32Process (f : float32 -> unit) (node: Node) =
    node |> addDelta32Process f false
    
let addPhysicsDelta32Process (f : float32 -> unit) (node: Node) =
    node |> addDelta32Process f true

let private removeProcessWith physics (id: Guid) (node: Node) =
    if node |> hasProcess physics |> not then
        false
    else
        let data = node |> getProcessData physics
        let success, _ = data.Process.Remove id
        success

let removeIdleProcess (id: Guid) (node: Node) =
    node |> removeProcessWith false id
    
let removePhysicsProcess (id: Guid) (node: Node) =
    node |> removeProcessWith true id

let removeProcess (id: Guid) (node: Node) =
    node |> removeIdleProcess id || node |> removePhysicsProcess id

type ProcessConfig =
    {
        Process : ProcessUnit
        Physics : bool
    }
    member this.AddWith (node : Node) =
        node |> addProcessType this.Process this.Physics
    static member New physics proc=
        {
            Process = proc
            Physics = physics
        }
    static member NewIdle (proc : ProcessUnit) =
        ProcessConfig.New false proc
    static member NewIdle (f : unit -> unit) =
        ProcessConfig.New false (Unit f)
    static member NewIdle (f : float -> unit) =
        ProcessConfig.New false (Delta f)
    static member NewIdle (f : float32 -> unit) =
        ProcessConfig.New false (Delta32 f)
    static member NewPhysics (proc : ProcessUnit) =
        ProcessConfig.New true proc
    static member NewPhysics (f : unit -> unit) =
        ProcessConfig.New true (Unit f)
    static member NewPhysics (f : float -> unit) =
        ProcessConfig.New true (Delta f)
    static member NewPhysics (f : float32 -> unit) =
        ProcessConfig.New true (Delta32 f)

let addProcessBy (config : ProcessConfig) (node: Node) =
    config.AddWith node

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

let private treeDoProcess physics (tree : SceneTree) =
    treeUpdateRemoveCache physics
    treeUpdateProcessCache physics
    let nodes, data, delta =
        if physics then
            cachedPhysicsProcessNodes,
            cachedPhysicsProcessData,
            tree.GetCurrentScene().GetPhysicsProcessDeltaTime()
        else
            cachedProcessNodes,
            cachedProcessData,
            tree.GetCurrentScene().GetProcessDeltaTime()
    nodes |> Seq.iter (fun n ->
        if GodotObject.IsInstanceValid n && n.CanProcess () then
            data[n].DoProcess delta
    )

// entry point

let private tree =
    lazy (
        // this is optional, it can increase runtime performance
        // otherwise script cache will be built on their first call
        FScript.buildCache ()
        
        let t = Engine.GetMainLoop () :?> SceneTree
        t.add_NodeAdded (fun node -> node |> FScript.init)
        t.add_ProcessFrame (fun () -> t |> treeDoProcess false)
        t.add_PhysicsFrame (fun () -> t |> treeDoProcess true)
        
        t
    )
    
let getTree () = tree.Value