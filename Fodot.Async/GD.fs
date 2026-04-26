module Fodot.Async.GD

open Fodot.Core

type GDSignal with
    member this.AsTask () =
        this.Object |> GodotObject.toSignal this.SignalName
        
    member this.AsTaskWith ct =
        this.Object |> GodotObject.toSignalWith ct this.SignalName