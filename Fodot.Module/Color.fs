module Fodot.Module.Color

open Godot

let getR (c: Color) =
    c.R
    
let getG (c: Color) =
    c.G
    
let getB (c: Color) =
    c.B
    
let getA (c: Color) =
    c.A
    
let setR (v : float32) (c: Color) =
    let mutable r = c
    r.R <- v
    r
    
let setG (v : float32) (c: Color) =
    let mutable r = c
    r.G <- v
    r
    
let setB (v : float32) (c: Color) =
    let mutable r = c
    r.B <- v
    r
    
let setA (v : float32) (c: Color) =
    let mutable r = c
    r.A <- v
    r
    
let getRGB (c: Color) =
    Vector3(c.R, c.G, c.B)
    
let setRGB (rgb : Vector3) (c: Color) =
    let mutable r = c
    r.R <- rgb.X
    r.G <- rgb.Y
    r.B <- rgb.Z
    r