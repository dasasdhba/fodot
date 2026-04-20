module Fodot.Core.Engine

open Godot

let private tree =
    lazy (
        let t = Engine.GetMainLoop () :?> SceneTree
        t.add_NodeAdded (fun node -> node |> FScript.init)
        t
    )
    
let getTree () = tree.Value