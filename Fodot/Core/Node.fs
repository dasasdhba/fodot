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
    
let addChildInternalFront (child : Node) (node : Node) =
    node |> addChildInternal child Node.InternalMode.Front

let addChildInternalBack (child : Node) (node : Node) =
    node |> addChildInternal child Node.InternalMode.Back

let addSibling (sibling : Node) (node : Node) =
    if node |> isAccessSafe then
        node.AddSibling(sibling)
    else
        node |> callDeferred "add_sibling" [| sibling |> Variant.from |]

// node get

let getNode<'a when 'a: not struct and 'a :> Node> (name : string) (node : Node) =
    node.GetNode<'a>(name)

let getSomeNode<'a when 'a: not struct and 'a :> Node> (name : string) (node : Node) =
    match node.GetNodeOrNull<'a> name with
    | null -> None
    | node -> Some node
    
let getChildInternal<'a when 'a: not struct and 'a :> Node> (idx : int) (node : Node) =
    node.GetChild<'a>(idx, true)
    
let getChild<'a when 'a: not struct and 'a :> Node> (idx : int) (node : Node) =
    node.GetChild<'a>(idx)

let private getSomeChildWith<'a when 'a: not struct and 'a :> Node> (idx: int) inter (node : Node) =
    match node.GetChild<'a>(idx, inter) with
    | null -> None
    | node -> Some node

let getSomeChild<'a when 'a: not struct and 'a :> Node> (idx : int) (node : Node) =
    node |> getSomeChildWith<'a> idx false

let getSomeChildInternal<'a when 'a: not struct and 'a :> Node> (idx : int) (node : Node) =
    node |> getSomeChildWith<'a> idx true

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

// bridge event

let bindChild (child : Node) (node : Node) =
    child.add_TreeEntered (fun _ ->
        if child.GetParent () <> node then
            child.QueueFree ()
    )

let createEventBy<'n, 'a when 'n :> Node> (child : 'n) signal (node: Node) =
    let event = Event<'a>()
    signal event.Trigger
    
    node |> bindChild child
    node |> addChildInternalFront child
    
    event.Publish

let createDeleteEvent (node: Node) =
    let child = new GodotBridge.PreDeleteBridge()
    node |> createEventBy child child.add_SignalPreDeleted

let createInputEvent (node: Node) =
    let child = new GodotBridge.InputBridge()
    node |> createEventBy child child.add_SignalInput

let createUnhandledInputEvent (node: Node) =
    let child = new GodotBridge.UnhandledInputBridge()
    node |> createEventBy child child.add_SignalUnhandledInput

// init
    
let initScripts (node: Node) =
    node |> FScript.init
    node |> getChildrenRec |> List.iter (fun c -> c |> FScript.init)