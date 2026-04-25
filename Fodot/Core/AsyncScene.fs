namespace Fodot.Core

open System
open System.Collections.Concurrent
open System.Threading.Tasks
open Godot
open GodotTask

type SceneOrPath =
    | Scene of PackedScene
    | Path of string
    
    member this.Packed = lazy (
        match this with
        | Scene scene -> scene
        | Path path -> GD.loadAs<PackedScene> path
    )
    
    member this.ResourcePath =
        match this with
        | Scene scene -> scene.ResourcePath
        | Path path -> path
        
type AsyncScenePool() =
    let queueLock = obj()
    let mutable queuedAdd : SceneOrPath list = []
    let mutable queuedRemove : SceneOrPath list = []
    let pool =
        ConcurrentDictionary<PackedScene, ConcurrentQueue<Node>> ()
    let mutable addTask : Task<unit> option = None
    
    member this.Update (scene : SceneOrPath) =
        let scene = scene.Packed.Value
        let node = PackedScene.instantiate scene
        
        pool.AddOrUpdate(
            scene, (fun s ->
                let r = ConcurrentQueue<Node>()
                r.Enqueue(node)
                r
            ), (fun s q ->
                q.Enqueue(node)
                q
            )
        ) |> ignore
    
    member this.UpdateMultiple (count : int) (scene : SceneOrPath) =
        for _ in 1..count do
            this.Update scene
    
    member this.Get (scene : SceneOrPath) =
        let scene = scene.Packed.Value
        let queue = pool.GetOrAdd(scene, fun s ->
            ConcurrentQueue<Node>()
        )
        
        let mutable node = null
        if queue.TryDequeue (&node) then
            Ok node
        else
            Logger.pushWarn $"{scene} at {scene.ResourcePath} has not been cached yet, try to increase initial count, or use pooling instead."
            PackedScene.instantiate scene |> Result.Error
    
    member private this.RemoveWith count (scene : SceneOrPath) =
        let matching, remain =
            queuedAdd |> List.filter (fun s -> s = scene),
            queuedAdd |> List.filter (fun s -> s <> scene)
        
        if matching.Length >= count then
            queuedAdd <- matching[count..] @ remain
        else
            queuedAdd <- remain
            
            let mutable remain = count - matching.Length
            let scene = scene.Packed.Value
            let queue = pool.GetOrAdd(scene, fun s ->
                ConcurrentQueue<Node>()
            )
            
            let mutable node = null
            while queue.TryDequeue (&node) && remain > 0 do
                node.QueueFree ()
                remain <- remain - 1
    
    member private this.Remove () =
        queuedRemove
        |> List.countBy id
        |> List.iter (fun (s, i) -> this.RemoveWith i s)
        
        queuedRemove <- []
    
    member private this.CreateAddTask () = task {
        do! GDTask.RunOnThreadPool(fun () ->
            while queuedAdd.Length > 0 do
                lock queueLock (fun () ->
                    let scene = queuedAdd.Head
                    this.Update scene
                    queuedAdd <- queuedAdd.Tail
                ) 
        )
    }
    
    member private this.CreateRemoveTask () = task {
        do! GDTask.RunOnThreadPool(fun () ->
            lock queueLock (fun () ->
                this.Remove ()
            )
        )
    }
    
    member this.AddList (scene : SceneOrPath list) =
        lock queueLock (fun () ->
            queuedAdd <- queuedAdd @ scene
        )
        
        if addTask.IsNone || addTask.Value.IsCompleted then
            addTask <- Some (this.CreateAddTask ())
    
    member this.AddMultiple (count : int) (scene : SceneOrPath) =
        this.AddList [for _ in 1..count -> scene]
    
    member this.Add (scene : SceneOrPath) =
        this.AddMultiple 1 scene
    
    member this.RemoveList (scene : SceneOrPath list) =
        lock queueLock (fun () ->
            queuedRemove <- queuedRemove @ scene
        )
        
        this.CreateRemoveTask () |> ignore
        
    member this.RemoveMultiple (count : int) (scene : SceneOrPath) =
        this.RemoveList [for _ in 1..count -> scene]
    
    member this.Remove (scene : SceneOrPath) =
        this.RemoveMultiple 1 scene
    
type AsyncSceneConfig =
    {
        Pool : AsyncScenePool
        Scene : SceneOrPath
        MaxCount : int
        InitialCount : int
    }

type AsyncScene<'a when 'a :> Node> (cfg : AsyncSceneConfig) =
    let mutable disposed = false
    
    do
        let initCount = min cfg.MaxCount cfg.InitialCount
        if initCount > 0 then
            cfg.Pool.UpdateMultiple initCount cfg.Scene
        
        let remain = cfg.MaxCount - initCount
        if remain > 0 then
            cfg.Pool.AddMultiple remain cfg.Scene
        
    member this.Get () =
        if disposed then
            failwith $"AsyncScene at {cfg.Scene.ResourcePath} has been disposed."
        
        match cfg.Pool.Get cfg.Scene with
        | Ok node ->
            cfg.Pool.Add cfg.Scene
            node :?> 'a
        | Result.Error node ->
            node :?> 'a
            
    member this.Dispose () =
        Logger.push "Async Scene Disposed."
        disposed <- true
        cfg.Pool.RemoveMultiple cfg.MaxCount cfg.Scene
        
    interface IDisposable with
        member this.Dispose () = this.Dispose ()
        
module AsyncScene =
    
    let globalPool = AsyncScenePool ()
    
    let createCfg (scene : SceneOrPath) (maxCount : int) (initialCount : int) =
        {
            Pool = globalPool
            Scene = scene
            MaxCount = maxCount
            InitialCount = initialCount
        }
    
    let createBy<'a when 'a :> Node> (cfg : AsyncSceneConfig) =
        new AsyncScene<'a>(cfg)
        
    let bind (node : Node) (scene : AsyncScene<'a>) =
        let del = node |> Node.createDeleteEvent
        del.Add (fun () -> scene.Dispose ())
        
    let createWith<'a when 'a :> Node> (node : Node) (cfg : AsyncSceneConfig) =
        let scene = createBy<'a> cfg
        scene |> bind node
        scene
        
type AsyncSceneConfig with
    member this.CreateWith<'a when 'a :> Node> node =
        AsyncScene.createWith<'a> node this
    static member From (scene : SceneOrPath) (maxCount : int) (initialCount : int) =
        AsyncScene.createCfg scene maxCount initialCount
    static member FromScene (scene : PackedScene) (maxCount : int) (initialCount : int) =
        AsyncScene.createCfg (Scene scene) maxCount initialCount
    static member FromPath (path : string) (maxCount : int) (initialCount : int) =
        AsyncScene.createCfg (Path path) maxCount initialCount