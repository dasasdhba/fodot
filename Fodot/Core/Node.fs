module Fodot.Core.Node

open Fodot.Core
open Fodot.Core.GodotObject
open Godot

// node access

let isAccessSafe (node : Node) =
    (node.IsInsideTree () |> not) || node.IsNodeReady ()

let addChildInternal (child : Node) inter (node : Node) =
    if node |> isAccessSafe then
        node.AddChild(child, false, inter)
    else
        node |> callDeferred "add_child" (child, inter)

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
        node |> callDeferred "add_sibling" sibling

let moveChild (idx : int) (node : Node) =
    if node |> isAccessSafe then
        node.MoveChild(node, idx)
    else
        node |> callDeferred "move_child" (node, idx)

// node get

let getNode<'a when 'a: not struct and 'a :> Node> (name : string) (node : Node) =
    node.GetNode<'a>(name)

let tryGetNode<'a when 'a: not struct and 'a :> Node> (name : string) (node : Node) =
    match node.GetNodeOrNull<'a> name with
    | null -> None
    | node -> Some node
    
let getChildInternal<'a when 'a: not struct and 'a :> Node> (idx : int) (node : Node) =
    node.GetChild<'a>(idx, true)
    
let getChild<'a when 'a: not struct and 'a :> Node> (idx : int) (node : Node) =
    node.GetChild<'a>(idx)

let private tryGetChildWith<'a when 'a: not struct and 'a :> Node> (idx: int) inter (node : Node) =
    match node.GetChildOrNull<'a>(idx, inter) with
    | null -> None
    | node -> Some node

let tryGetChild<'a when 'a: not struct and 'a :> Node> (idx : int) (node : Node) =
    node |> tryGetChildWith<'a> idx false

let tryGetChildInternal<'a when 'a: not struct and 'a :> Node> (idx : int) (node : Node) =
    node |> tryGetChildWith<'a> idx true

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
            child.SetBlockSignals true
            child.QueueFree ()
    )

let createEventBy<'n, 'a when 'n :> Node> (child : 'n) signal (node: Node) =
    let event = Event<'a>()
    signal event.Trigger
    
    node |> bindChild child
    node |> addChildInternalFront child
    
    event.Publish

let createDeleteEvent (node: Node) =
    let child = new GodotBridge.DeleteBridge()
    node |> createEventBy child child.add_SignalDeleted

let createInputEvent (node: Node) =
    let child = new GodotBridge.InputBridge()
    node |> createEventBy child child.add_SignalInput

let createUnhandledInputEvent (node: Node) =
    let child = new GodotBridge.UnhandledInputBridge()
    node |> createEventBy child child.add_SignalUnhandledInput

type private CachedEvent () =
    inherit RefCounted ()
    
    member val DeleteEvent : IEvent<unit> option = None with get, set
    member val InputEvent : IEvent<InputEvent> option = None with get, set
    member val UnhandledInputEvent : IEvent<InputEvent> option = None with get, set
    
let private getCachedEventWith getter setter creator node =
    let cache = node |> getMetaWithDefaultAs "_fs_node_cached_event" (lazy new CachedEvent())
    match getter cache with
    | Some event -> event
    | None ->
        let event = creator ()
        setter cache event
        event

let getDeleteEvent (node: Node) =
    node
    |> getCachedEventWith
        _.DeleteEvent
        (fun cache event -> cache.DeleteEvent <- Some event)
        (fun () -> node |> createDeleteEvent)

let getInputEvent (node: Node) =
    node
    |> getCachedEventWith
        _.InputEvent
        (fun cache event -> cache.InputEvent <- Some event)
        (fun () -> node |> createInputEvent)

let getUnhandledInputEvent (node: Node) =
    node
    |> getCachedEventWith
        _.UnhandledInputEvent
        (fun cache event -> cache.UnhandledInputEvent <- Some event)
        (fun () -> node |> createUnhandledInputEvent)

// init
    
let initScripts (node: Node) =
    node |> FScript.init
    node |> getChildrenRec |> List.iter (fun c -> c |> FScript.init)