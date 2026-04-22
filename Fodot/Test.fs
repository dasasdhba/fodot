module Fodot.Test

open Godot
open Fodot.Core
open Fodot.Core.GodotObject

[<FScript("test_script")>]
type TestScript(node : Node2D) =    
    do
        node |> Engine.addPhysicsDelta32Process (fun delta ->
            node.Position <- node.Position + 100f * delta * Vector2.Right
        ) |> ignore
    
    member val TestData = "哇哈哈"
    member this.TestName
        with get () = node |> get<string> "name"
        and set (v: string) = node |> set "name" v