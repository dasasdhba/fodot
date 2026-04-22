module Fodot.Test

open Godot
open Fodot.Core
open Fodot.Core.GodotObject

[<FScript("test_script")>]
type TestScript(node : Node2D) =
    let testProp =
        let name = (node.Get "name").AsStringName ()
        GD.Print $"Hello from FSharp script, in node {name}"
        name
        
    let testGetData () =
        let script = node |> FScript.get<TestScript>
        match script with
        
        | Ok s ->
            GD.Print $"Get script data {s.TestData}"
            GD.Print $"Access node name from script data: {s.TestName}"
        | Error e ->
            GD.Print $"Get script data failed: {e}"
            
    let testGetAfterReady=
        node.add_Ready (fun () -> testGetData ())
    
    do
        node |> Engine.addPhysicsDelta32Process (fun delta ->
            node.Position <- node.Position + 100f * delta * Vector2.Right
        ) |> ignore
    
    member val TestData = "哇哈哈"
    member this.TestName
        with get () = node |> get<string> "name"
        and set (v: string) = node |> set "name" v