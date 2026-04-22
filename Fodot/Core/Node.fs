module Fodot.Core.Node

open Fodot.Core.GodotObject
open Godot

// node add

let isAccessSafe (node : Node) =
    (node.IsInsideTree () |> not) || node.IsNodeReady ()

let addChildInternal (child : Node) inter (node : Node) =
    if node |> isAccessSafe then
        node.AddChild(child, false, inter)
    else
        node |> callDeferred "add_child" [| child |> Variant.from; inter |> Variant.from |]

let addChild (child : Node) (node : Node) =
    node |> addChildInternal child Node.InternalMode.Disabled
    
let addSibling (sibling : Node) (node : Node) =
    if node |> isAccessSafe then
        node.AddSibling(sibling)
    else
        node |> callDeferred "add_sibling" [| sibling |> Variant.from |]

// node get

let rec getChildrenRecWith filter (node: Node) =
    node.GetChildren ()
    
    |> List.ofSeq
    |> List.fold (fun acc child ->
        let acc = if filter child then acc @ [child] else acc
        acc |> List.append (child |> getChildrenRecWith filter)
    ) []

let getChildrenRec (node: Node) =
    node |> getChildrenRecWith (fun _ -> true)
    
let getChildrenAndSelfRecWith filter (node: Node) =
    let children = node |> getChildrenRecWith filter
    if filter node then
        node :: children
    else
        children

let getChildrenAndSelfRec (node: Node) =
    node |> getChildrenAndSelfRecWith (fun _ -> true)

let bindChild (child : Node) (node : Node) =
    node.add_TreeEntered (fun _ ->
        if child.GetParent () <> node then
            child.QueueFree ()
    )
    node.add_TreeExited (fun _ -> child.QueueFree ())

// init
    
let initScripts (node: Node) =
    node |> FScript.init
    node |> getChildrenRec |> List.iter (fun c -> c |> FScript.init)