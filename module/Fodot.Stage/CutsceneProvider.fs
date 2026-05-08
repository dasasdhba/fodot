namespace Fodot.Stage

open Fodot.Async
open Fodot.Common
open Fodot.Core
open Godot

[<FScript("cutscene_provider")>]
type CutsceneProvider(node : Node) =
    let bind = Bind.CutsceneProvider node
    
    let poolMapper s =
        node |> AsyncScene.fromNode s 1 0
    
    let inPool = bind.InScene |> Option.map poolMapper
    let outPool = bind.OutScene |> Option.map poolMapper
    
    let nodeMapper (p :AsyncScene<Node>) =
        p.Get() |> FScript.get<ICutscene>

    member this.CreateConfig () : CutsceneConfig =
        let inNode = inPool |> Option.map nodeMapper
        let outNode =
            match outPool with
            | Some p -> nodeMapper p |> Some
            | None -> if bind.ReuseInIfOutIsNull then inNode else None
        
        {
            In = inNode
            Out = outNode
        }