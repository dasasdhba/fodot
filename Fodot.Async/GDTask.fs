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

let runOnThreadWith<'a> ct (f: unit -> 'a) = task {
    let! result = GDTask.RunOnThreadPool(f, false, ct)
    return result
}

let runOnThread<'a> (f: unit -> 'a) =
    runOnThreadWith CancellationToken.None f

let toThreadWith ct physics= task {
    let timing =
        if physics then
            PlayerLoopTiming.IsolatedPhysicsProcess
        else
            PlayerLoopTiming.IsolatedProcess
    do! GDTask.SwitchToMainThread(timing, ct) 
}

let toThread physics =
    toThreadWith CancellationToken.None physics

let toIdleThreadWith ct =
    toThreadWith ct false
    
let toPhysicsThreadWith ct =
    toThreadWith ct true
    
let toIdleThread () =
    toThread false
    
let toPhysicsThread () =
    toThread true