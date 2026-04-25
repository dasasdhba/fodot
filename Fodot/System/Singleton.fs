module Fodot.System.Singleton

open Fodot.Core

let attach (node : 'a) (instance : byref<'a>) =
    match instance with
    | null ->
        instance <- node
        true
    | _ ->
        Logger.pushWarn "F# singleton already exists. Are you running a singleton scene directly from editor? Then something might not work correctly."
        false