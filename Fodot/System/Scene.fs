module Fodot.System.Scene

open Fodot
open Fodot.Core
open Godot

let mutable private instance : Node = null
let getSingleton () = instance

[<FScript("scene_singleton")>]
type SceneSingleton (node : Node) =
    do if Singleton.attach node &instance then
        Logger.push "Scene singleton loaded."