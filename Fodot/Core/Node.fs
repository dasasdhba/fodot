module Fodot.Core.Node

open Godot

let rec getChildrenRecWith filter (node: Node) =
    let children =
        node.GetChildren ()
        
        |> List.ofSeq
        |> List.filter filter
    
    children
    
    |> List.fold (fun acc child -> 
        acc |> List.append (child |> getChildrenRecWith filter)
    ) children

let getChildrenRec (node: Node) =
    node |> getChildrenRecWith (fun _ -> true)
    
let initScripts (node: Node) =
    node |> FScript.init
    node |> getChildrenRec |> List.iter (fun c -> c |> FScript.init)