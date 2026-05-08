module Fodot.Stage.GlobalStage

open Fodot.Stage.Stage
open Godot
open Fodot.Common
open Fodot.Core
open Godot.Common

// this one works fine for single stage game
// for multiple stage case, it's encouraged to write your own global stage
// and set up some debug config for easy testing

let mutable private instance : Control = null

let getInstance () =
    instance
    |> Option.ofObj
    |> Option.bind (fun c -> c |> FScript.tryGet<Stage>)
    |> Option.defaultWith (fun _ -> failwith "GlobalStage singleton is not created yet.")
    
[<FScript("global_stage")>]
type GlobalStage (node : Control) =
    do if Singleton.attach node &instance then
        Logger.push "GlobalStage loaded."
    
    let entryCutscene = "%EntryCutscene"
        
    do node.add_Ready (fun _ ->
        let first =
#if TOOLS
            let file = FileAccess.Open(FodotEditor.DebugScenePath, FileAccess.ModeFlags.Read)
            using file _.GetLine()
#else
            FodotEditor.ProjectMainScene
#endif

        let stage = node |> FScript.attach<Stage>
        let cutscene = node |> Node.getCutsceneConfig entryCutscene
        stage |> queueChangeScene first cutscene |> ignore
    )