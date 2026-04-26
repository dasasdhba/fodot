module Fodot.Async.GodotObject

open Godot

// signal

let toSignal (name : string) (obj : GodotObject) =
    GodotTask.GDTask.FromSignal(obj, name)

let toSignalWith ct (name : string) (obj : GodotObject) =
    GodotTask.GDTask.FromSignal(obj, name, ct)