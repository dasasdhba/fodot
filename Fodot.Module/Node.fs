module Fodot.Module.Node

open Godot
open Fodot.Core

let rec findParentWith<'a when 'a : null and 'a :> Node> filter (node : Node) =
    match node |> Node.tryGetParent<'a> with
    | None -> None
    | Some p when filter p -> Some p
    | Some p -> p |> findParentWith<'a> filter
    
let findParent<'a when 'a : null and 'a :> Node> (node : Node) =
    node |> findParentWith<'a> (fun _ -> true)
    
let rec findParentCachedWith<'a when 'a : null and 'a :> Node> filter meta (node : Node) =
    if node |> GodotObject.hasMeta meta then
        node |> GodotObject.getMetaAs<'a> meta |> Some
    else
        let result =
            match node |> Node.tryGetParent<'a> with
            | None -> None
            | Some p when filter p -> Some p
            | Some p -> p |> findParentCachedWith<'a> filter meta
        if result |> Option.isSome then
            node |> GodotObject.setMeta meta result.Value
        result
        
let findParentCached<'a when 'a : null and 'a :> Node> meta (node : Node) =
    node |> findParentCachedWith<'a> (fun _ -> true) meta