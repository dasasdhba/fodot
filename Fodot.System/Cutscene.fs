namespace Fodot.System

open Fodot.Common
open Fodot.Core
open Fodot.Module
open Godot

type ICutscene =
    abstract member SetSize : Vector2 -> unit
    abstract member SetProgress : float -> unit

[<FScript("cutscene_color")>]
type CutsceneColorScript(node : Node2D) =
    let size = node |> GDProp<Vector2>.From "size"
    let color = node |> GDProp<Color>.From "color"
    
    member this.Size
        with get() = size.Get ()
        and set value = size.Set value
    
    member this.Color
        with get() = color.Get ()
        and set value = color.Set value
        
    interface ICutscene with
        member this.SetSize value =
            this.Size <- value
        member this.SetProgress p =
            node.SetModulate(node.Modulate |> Color.setA (float32 p))

type ICutsceneConfig =
    abstract member Key : string
    abstract member Init : ICutscene -> unit

type CutsceneTime =
    {
        FadeIn : float
        Wait : float
        FadeOut : float
    }
    static member Default=
        {
            FadeIn = 0.4
            Wait = 0.2
            FadeOut = 0.4
        }

type CutsceneConfig =
    {
        Cutscene : ICutsceneConfig
        Time : CutsceneTime
        Tween : TweenConfig
    }