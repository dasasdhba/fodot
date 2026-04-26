module Fodot.Test

open System.Threading
open Fodot.Common
open Fodot.Core.Engine
open Fodot.Core.Node
open Godot
open Fodot.Async
open Fodot.Core.GodotObject

type MyResource =
    {
        my_int : int64
        my_str : string
        my_array : Collections.Array
    }

[<FScript("test_script")>]
type TestScript(node : Node2D) =    
    do
        let proc = ProcessConfig.NewPhysics (fun delta ->
            node.Position <- node.Position + 100f * delta * Vector2.Right
        )
        proc.AddWith node |> ignore
    
    let scene = node |> get<PackedScene> "scene"
    let loader = node |> AsyncScene.fromNode<Node2D> scene 5 0
    
    let task ()= task {
        let async = AsyncNode.NewPhysics node CancellationToken.None
        do! 1.0 |> async.Delay
        node.Position <- node.Position + 100f * Vector2.Down
        
        do! 1.0 |> async.Delay
        let n2 = loader.Get()
        n2.Position <- node.Position + 100f * Vector2.Up
        node |> addSibling n2
        
        do! 1.0 |> async.Delay
        node.QueueFree ()
    }
    
    do
        node.add_Ready (fun () ->
            task () |> ignore
            let res = node |> get<Resource> "res"
            let record = res |> deserialize<MyResource>
            GD.Print record.my_str
        )
    
    member val TestData = "哇哈哈"
    member this.TestName
        with get () = node |> get<string> "name"
        and set (v: string) = node |> set "name" v