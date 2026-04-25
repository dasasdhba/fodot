module Fodot.Core.Logger

open Godot

let push (message: string) =
    GD.Print $"[{System.DateTime.Now}] {message}"
    
let pushWarn (message: string) =
    GD.PushWarning $"[{System.DateTime.Now}] {message}"
    
let pushError (message: string) =
    GD.PushError $"[{System.DateTime.Now}] {message}"