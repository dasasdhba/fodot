module Lib.Core.PackedScene

open Godot
open Lib.Core.Node

let private instantiateLock = obj()

let instantiateWith gen (packedScene: PackedScene) =
    let node = 
        lock instantiateLock (fun () ->
            packedScene.Instantiate(gen)
        )
    node |> getChildrenRec |> List.iter FScript.init
    node
    
let instantiate (packedScene: PackedScene)  =
    let node = 
        lock instantiateLock (fun () ->
            packedScene.Instantiate()
        )
    node |> getChildrenRec |> List.iter FScript.init
    node