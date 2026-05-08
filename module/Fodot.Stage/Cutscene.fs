namespace Fodot.Stage

open System.Threading.Tasks
open Fodot.Async.GD
open Fodot.Common
open Fodot.Core
open Godot

type ICutscene =
    abstract member Root : Node2D
    abstract member SetSize : Vector2 -> unit
    abstract member FadeInInit : unit -> unit
    abstract member FadeOutInit : unit -> unit
    abstract member FadeIn : unit -> Task<unit>
    abstract member FadeOut : unit -> Task<unit>

type CutsceneConfig =
    {
        In : ICutscene option
        Out : ICutscene option
    }

    static member From cutscene = {
        In = Some cutscene
        Out = Some cutscene
    }

    static member None = {
        In = None
        Out = None
    }

[<FScript("cutscene")>]
type Cutscene(node : Node2D) =
    let size = node |> GDProp<Vector2>.From "size"
    let fadeInFinished = node |> GDSignal<unit>.From "fade_in_finished"
    let fadeOutFinished = node |> GDSignal<unit>.From "fade_out_finished"

    member this.Size
        with get() = size.Get ()
        and set value = size.Set value

    interface ICutscene with
        member this.Root = node
        
        member this.SetSize value =
            this.Size <- value
            
        member this.FadeInInit () =
            node |> GodotObject.tryInvoke "fade_in_init" |> ignore
            
        member this.FadeOutInit () =
            node |> GodotObject.tryInvoke "fade_out_init" |> ignore
            
        member this.FadeIn () = task {
            node |> GodotObject.invoke "fade_in" |> ignore
            let! _ = fadeInFinished.AsTask()
            return ()
        }
        
        member this.FadeOut () = task {
            node |> GodotObject.invoke "fade_out" |> ignore
            let! _ = fadeOutFinished.AsTask()
            return ()
        }