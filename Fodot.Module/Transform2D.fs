module Fodot.Module.Transform2D

open Godot

let getOrigin (transform: Transform2D) =
    transform.Origin
    
let getRotation (transform: Transform2D) =
    transform.Rotation
    
let getScale (transform: Transform2D) =
    transform.Scale
    
let getSkew (transform: Transform2D) =
    transform.Skew
    
let setOrigin (origin: Vector2) (transform: Transform2D) =
    let mutable result = transform
    result.Origin <- origin
    result
    
let setRotation (rotation: float32) (transform: Transform2D) =
    Transform2D(rotation, transform.Scale, transform.Skew, transform.Origin)
    
let setScale (scale: Vector2) (transform: Transform2D) =
    Transform2D(transform.Rotation, scale, transform.Skew, transform.Origin)
    
let setSkew (skew: float32) (transform: Transform2D) =
    Transform2D(transform.Rotation, transform.Scale, skew, transform.Origin)