module Lib.Test

open FSharpPlus
open Godot
open Lib.Core

[<FScript("test_script")>]
type TestScript(node : Node) =
    let testProp =
        let name = (node.Get "name").AsStringName ()
        GD.Print $"Hello from FSharp script, in node {name}"
        name
        
    let testExport () = monad {
        let! ex = node |> Register.getExportedNode "test_node"
        GD.Print $"Exported node: {ex.Name}"
    }
    
    let testAfterReady =
        node.add_Ready (fun () ->
            GD.Print "Ready."
            testExport () |> ignore
        )