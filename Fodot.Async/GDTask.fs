module Fodot.Async.GDTask

open System.Threading
open System.Threading.Tasks
open GodotTask

// fsharp event

let awaitEvent (event: IEvent<'Delegate, 'Args>) (ct: CancellationToken) =
    let tcs = TaskCompletionSource<'Args>()
    
    let subscription = event.Subscribe(fun args -> 
        tcs.TrySetResult(args) |> ignore
    )

    let registration = ct.Register(fun () -> 
        tcs.TrySetCanceled() |> ignore
    )

    task {
        try
            return! tcs.Task
        finally
            subscription.Dispose()
            registration.Dispose()
    }

// thread control

let toThread physics ct=
    let timing =
        if physics then
            PlayerLoopTiming.IsolatedPhysicsProcess
        else
            PlayerLoopTiming.IsolatedProcess
    GDTask.SwitchToMainThread(timing, ct) 

let toIdleThread ct =
    toThread false ct
    
let toPhysicsThread ct =
    toThread true ct