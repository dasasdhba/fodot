module Lib.Core.Register

open System
open Godot
open FSharpPlus
open Lib.Core.Script
open Lib.Moon.GodotObject

let private ownerRegisterNode (owner: GodotObject) (node: Node) (exName: string) =
    let name = $"_ex_{exName}"
    let dict =
        owner
        
        |> getMetaWithDefault name (lazy 
           Collections.Dictionary<string, Node>()
        )
    
    let key = monad {
        let! str = node |> getSomeMeta<string> name
        if dict.ContainsKey str then
#if DEBUG    
            GD.PushWarning($"{owner}: failed to register {node}, as there exists the same export key: {str}");
#endif
            return! None
        else
            str
    }
    
    let key = key |> Option.defaultWith (fun () -> Guid.NewGuid().ToString ())
    dict.Add(key, node)
    
    node.add_TreeExited (fun () -> dict.Remove(key) |> ignore)

let rec private getOwnerRec (node: Node) =
    match node.GetOwner() with
    
    | owner when owner <> null ->
        owner
    | _ ->
        let p = node.GetParent()
        match p with
        
        | parent when parent <> null -> parent |> getOwnerRec
        | _ -> node

let private getMetaAndGroupListWith filter (obj : GodotObject) =
    obj.GetMetaList()
    
    |> List.ofSeq
    
    |> List.append (
        match obj with
        | :? Node as n -> n.GetGroups () |> List.ofSeq
        | _ -> []
    )
    
    |> List.choose (fun m ->
        let s = m |> string
        if s |> filter then
            Some s
        else
            None
    )

let getExportedNodeWithKey (key : string) (exName : string) (node : Node) =
    let owner = getOwnerRec node
    monad {
        let! dict = owner |> getSomeMeta<Collections.Dictionary<string, Node>> $"_ex_{exName}"
        if dict.ContainsKey key then
            dict[key]
        else
            return! dict.Values |> Seq.tryHead
    }

let getExportedNode (exName : string) (node : Node) =
    node |> getExportedNodeWithKey "" exName

let registerToOwner (node : Node) =
    let owner = getOwnerRec node
    let arr = node |> getMetaAndGroupListWith (fun s -> s.StartsWith "ex_" && s.Length > 3)
    
    for m in arr do
        let exName = m[3..]
        ownerRegisterNode owner node exName
        
let registerScript (obj : GodotObject) =
    let arr = obj |> getMetaAndGroupListWith (fun s -> s.StartsWith "fs_" && s.Length > 3)
    
    for m in arr do
        let fsName = m[3..]
        
        try
            match createScript fsName [|obj|] with
            
            | Some script ->
                ()
            | None ->
                raise (Exception "the script was not found is F# library")
        with
        
        | ex -> GD.PushError $"{obj}: failed to create script {fsName}: {ex}"