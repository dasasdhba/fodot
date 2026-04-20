namespace Fodot.System

open Fodot.Core
open Godot

type SceneManagerData(node : Node) =
    member this.Viewport : GDPropArray<NodePath> =
        node |> GDPropArray.From "viewports"

[<FScript("scene_manager")>]
type SceneManager(node : Node) =
    do
        node.add_Ready (fun () ->
            let data = SceneManagerData(node)
            for viewport in data.Viewport.Get () do
                GD.Print (node.GetNode viewport)
        )
    
module SceneManager =
    
    do 1+1 |> ignore