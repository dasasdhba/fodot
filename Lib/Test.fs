module Lib.Test

open Godot
open Lib.Core

[<FScript("test_script")>]
type TestScript(node : Node) =
    let testProp =
        let name = (node.Get "name").AsStringName ()
        GD.Print $"Hello from FSharp script, in node {name}"
        name
        
    let testGetData () =
        let script = node |> FScript.get<TestScript>
        match script with
        
        | Ok s ->
            GD.Print $"Get script data {s.TestData}"
        | Error e ->
            GD.Print $"Get script data failed: {e}"
            
    let testGetAfterReady=
        node.add_Ready (fun () -> testGetData ())
    
    member val TestData = "哇哈哈"