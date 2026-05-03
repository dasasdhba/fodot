module Fodot.Async.Tween

open Godot

let asTask (tween: Tween) = task {
    let! _ = tween |> GodotObject.toSignal "finished"
}

let asTaskWith ct (tween: Tween) = task {
    let! _ = tween |> GodotObject.toSignalWith ct "finished"
}