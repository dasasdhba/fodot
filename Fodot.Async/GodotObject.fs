module Fodot.Async.GodotObject

open System.Threading
open Godot

// signal

let toSignalWith ct (name : string) (obj : GodotObject) = task {
    let! result = GodotTask.GDTask.FromSignal(obj, name, ct)
    return result
}

let toSignal (name : string) (obj : GodotObject) =
    obj |> toSignalWith CancellationToken.None name