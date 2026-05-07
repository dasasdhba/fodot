namespace Fodot.Stage

open System.Threading.Tasks
open Fodot.Async
open Fodot.Core
open Godot
open Fodot.Common

type StageStatus =
     | Pending
     | Loading
     | Ready

[<FScript("stage")>]
type Stage(node : Control) =
     let viewport : Node option = node |> Node.tryGetNode "%Viewport"
     let cutscene : Node option = node |> Node.tryGetNode "%Cutscene"
     
     member val Root = node with get
     member this.Viewport
          with get () =
               match viewport with
               | Some v -> v
               | None -> node
     member this.CutsceneRoot
          with get () =
               match cutscene with
               | Some c -> c
               | None -> node
     member val Status = Pending with get, set
     member val CurrentScene : Node option = None with get, set
     member this.CurrentScenePath
          with get () =
               match this.CurrentScene with
               | Some n -> n.SceneFilePath
               | None -> ""

module Stage =
     
     // cutscene
     
     let private initCutscene (cutscene : ICutscene) (stage : Stage) = task {
          let node = cutscene.Root
          node.SetPosition Vector2.Zero
          cutscene.SetSize stage.Root.Size
          
          if node.GetParent() <> stage.CutsceneRoot then
               let waiting = node.ToSignalTreeEntered ()
               if node.IsInsideTree() |> not then
                    stage.CutsceneRoot |> Node.addChild node
               else
                    node |> Node.reparentDirectly stage.CutsceneRoot
               do! waiting
     }
     
     let fadeIn (cutscene : ICutscene) (stage : Stage) =
          cutscene.FadeInInit ()
          task {
               do! stage |> initCutscene cutscene
               do! cutscene.FadeIn ()
          }
          
     let fadeOut (cutscene : ICutscene) (stage : Stage) =
          cutscene.FadeOutInit ()
          task {
               do! stage |> initCutscene cutscene
               do! cutscene.FadeOut ()
          }
          
     let fadeInOutWith (middleTask: Task) (cutscene : CutsceneConfig) (stage : Stage) = task {
          match cutscene.In with
          | Some i -> do! stage |> fadeIn i
          | None -> ()
          
          do! middleTask
          
          match cutscene.Out with
          | Some o ->
               do! stage |> fadeOut o
               o.Root.QueueFree ()
          | None -> ()
          
          cutscene.In
          |> Option.map (fun i -> i.Root.QueueFree ())
          |> ignore
     }
     
     // load scene
     
     let asRelativePath (path : string) (current : string)=
          match path with
          
          | s when s.StartsWith('@') ->
               let body = s[1..]
               
               let dir = current.GetBaseDir ()
               let name = current.GetBaseName ()
               let ext = current.GetExtension ()
               
               let idx = name.LastIndexOf '_'
               let name = if idx < 0 then name else name[..(idx - 1)]
               
               dir + "/" + name + "_" + body + ext
          
          | "" -> current
          | s -> s
          
     let loadScene (path : string) (stage : Stage) =
          let path = stage.CurrentScenePath |> asRelativePath path
          GDTask.runOnThread (fun () ->
               GD.loadAs<PackedScene> path |> PackedScene.instantiate
          )
     
     let changeSceneTo (sceneLoader: Task<Node>) (stage : Stage) = task {
          let! scene = sceneLoader
          do! GDTask.toPhysicsThread ()
          
          match stage.CurrentScene with
          | Some n ->
               n.QueueFree ()
               if n.IsInsideTree () then
                    do! n.ToSignalTreeExited ()
          | None -> ()
          
          let wait = scene.ToSignalReady ()
          stage.CurrentScene <- Some scene
          stage.Viewport |> Node.addChild scene
          do! wait
          stage.Status <- Ready
          Logger.push $"Stage {stage.Root.GetPath()} is now at {stage.CurrentScenePath}"
     }
     
     let changeSceneWith (sceneLoader: Task<Node>) (cutscene : CutsceneConfig) (stage : Stage) =
          stage |> fadeInOutWith (stage |> changeSceneTo sceneLoader) cutscene

     let changeScene (path : string) (cutscene : CutsceneConfig) (stage : Stage) =
          stage.Status <- Loading
          stage |> changeSceneWith (stage |> loadScene path) cutscene
          
     let queueChangeScene (path : string) (cutscene : CutsceneConfig) (stage : Stage) = task {
          if stage.Status = Loading then
          #if TOOLS
               Logger.pushWarn $"Stage {stage.Root.GetPath()} has been queued for changing scene before, changing task to {path} will be cancelled."
          #endif     
               ()
          else
               do! stage |> changeScene path cutscene
     }
     
     let queueReload cutscene stage =
          stage |> queueChangeScene "" cutscene
