module Fodot.Core.PackedScene

open Godot
open Fodot.Core.Node

let private instantiateLock = obj()

let instantiateWith gen (packedScene: PackedScene) =
    let node = 
        lock instantiateLock (fun () ->
            packedScene.Instantiate(gen)
        )
    node |> initScripts
    node
    
let instantiate (packedScene: PackedScene)  =
    let node = 
        lock instantiateLock (fun () ->
            packedScene.Instantiate()
        )
    node |> initScripts
    node
    
let instantiateToWith<'a when 'a :> Node> gen (packedScene: PackedScene)=
    let node = packedScene |> instantiateWith gen
    node :?> 'a
    
let instantiateTo<'a when 'a :> Node> (packedScene: PackedScene) =
    let node = packedScene |> instantiate
    node :?> 'a