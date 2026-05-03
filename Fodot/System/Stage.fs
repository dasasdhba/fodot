namespace Fodot.System

open GodotTask
open Fodot.Core
open Godot
open Fodot.Common

type StageStatus =
     | Pending
     | Loading
     | Ready
     | Exiting
     | Exited

[<FScript("stage")>]
type Stage(node : Control) =
     let viewport = node |> Node.getNode<Viewport> "%Viewport"
     let mutable status = Pending
     let mutable sceneRoot : Node = null
     let mutable scenePath : string = ""
     
     member val Root = node with get
     member val Viewport = viewport with get
     member val Status = status with get
     member val Scene = sceneRoot with get
     member val ScenePath = scenePath with get

module Stage =
     
     let asRelativePath (path : string) (current : string)=
          match path with
          
          | s when s.StartsWith('@') ->
               let body = s[1..]
               
               let dir = current.GetBaseDir ()
               let name = current.GetBaseName ()
               let ext = current.GetExtension ()
               
               let idx = name.LastIndexOf '_'
               let name = if idx < 0 then name else name[..idx]
               
               dir + "/" + name + "_" + body + ext
          
          | "" -> current
          | s -> s
          
     let loadScene (path : string) (stage : Stage) =
          let path = stage.ScenePath |> asRelativePath path
          GDTask.RunOnThreadPool (fun () ->
               GD.loadAs<PackedScene> path |> PackedScene.instantiate
          )
