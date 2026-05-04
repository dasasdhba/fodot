namespace Fodot.System.Library

open Fodot.Core
open Godot

// res://module/Fodot.System/cutscene.tres
module Cutscene =
    let private _back_lib = GDLib("uid://by2q4qqhevgok")

    let color = _back_lib.Get<Resource>("color")
    let block = _back_lib.Get<Resource>("block")

    let lib = _back_lib.Lib
    let all : Resource list = [color; block]