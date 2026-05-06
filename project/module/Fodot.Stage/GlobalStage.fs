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
    |> Option.map (fun c -> c |> FScript.get<Stage>)
    |> Option.defaultWith (fun _ -> failwith "GlobalStage singleton is not created yet.")
    
[<FScript("global_stage")>]
type GlobalStage (node : Control) =
    do if Singleton.attach node &instance then
        Logger.push "GlobalStage loaded."
    
    let entryCutscene =
        node
        |> Node.tryGetNode "%EntryCutscene"
        |> Option.map (fun n -> n |> FScript.get<CutsceneProvider>)
        
    let getEntryCutscene () =
        entryCutscene
        |> Option.map (fun c -> c.CreateConfig ())
        |> Option.defaultValue CutsceneConfig.None
        
    do node.add_Ready (fun _ ->
        let first =
#if TOOLS
            let file = FileAccess.Open(FodotEditor.DebugScenePath, FileAccess.ModeFlags.Read)
            using file _.GetLine()
#else
            FodotEditor.ProjectMainScene
#endif
        let stage = node |> FScript.get<Stage>
        let cutscene = getEntryCutscene ()
        stage |> queueChangeScene first cutscene |> ignore
    )