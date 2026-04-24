module Fodot.Core.GDTask

open GodotTask

let toThread physics ct= task {
    let timing =
        if physics then
            PlayerLoopTiming.IsolatedPhysicsProcess
        else
            PlayerLoopTiming.IsolatedPhysicsProcess
    do! GDTask.SwitchToMainThread(timing, ct) 
}

let toIdleThread ct =
    toThread false ct
    
let toPhysicsThread ct =
    toThread true ct