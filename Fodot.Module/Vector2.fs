module Fodot.Module.Vector2

open Godot

let getX (v : Vector2) =
    v.X
    
let getY (v : Vector2) =
    v.Y
    
let setX (v : Vector2) (x : float32) =
    let mutable r = v
    r.X <- x
    r
    
let setY (v : Vector2) (y : float32) =
    let mutable r = v
    r.Y <- y
    r