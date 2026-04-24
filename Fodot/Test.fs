module Fodot.Test

open System.Threading
open Fodot.Core.Engine
open Godot
open Fodot.Core
open Fodot.Core.GodotObject

[<FScript("test_script")>]
type TestScript(node : Node2D) =    
    do
        let proc = ProcessConfig.NewPhysics (fun delta ->
            node.Position <- node.Position + 100f * delta * Vector2.Right
        )
        proc.AddWith node |> ignore
    
    let task ()= task {
        let async = AsyncNode.NewPhysics node CancellationToken.None
        do! 3.0 |> async.Delay
        node.Position <- node.Position + 100f * Vector2.Down
    }
    
    do
        node.add_Ready (fun () ->
            task () |> ignore
        )
    
    member val TestData = "哇哈哈"
    member this.TestName
        with get () = node |> get<string> "name"
        and set (v: string) = node |> set "name" v