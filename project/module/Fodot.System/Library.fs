namespace Fodot.System.Library

open Fodot.Core
open Godot
open Godot.Collections

// res://module/Fodot.System/cutscene.tres
module Cutscene =
    let private _back_lib = GDLib("uid://by2q4qqhevgok")

    let color = _back_lib.Get<Resource>("color")

    let lib = _back_lib.Lib
