namespace Fodot.System

type ICutscene =
    abstract member SetProgress : float -> unit
    
type CutsceneConfig =
    {
        Cutscene : ICutscene
        FadeInTime : float
        WaitTime : float
        FadeOutTime : float
    }

