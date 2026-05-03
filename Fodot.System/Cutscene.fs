namespace Fodot.System

open Godot

type ICutscene =
    abstract member SetSize : Vector2 -> unit
    abstract member SetProgress : float -> unit
    
type CutsceneConfig =
    {
        Cutscene : ICutscene
        FadeInTime : float
        WaitTime : float
        FadeOutTime : float
    }

