module Fodot.Module.CanvasItem

open Fodot.Core
open Godot

// transform

let getTransform (item : CanvasItem) =
    item.GetTransform()

let setTransform (v : Transform2D)  (item : CanvasItem)=
    item |> GodotObject.set "transform" v

let getPosition (item : CanvasItem) =
    item |> getTransform |> Transform2D.getOrigin

let setPosition pos item =
    item |> setTransform (item |> getTransform |> Transform2D.setOrigin pos)

let getRotation (item : CanvasItem) =
    item |> getTransform |> Transform2D.getRotation

let setRotation rot item =
    item |> setTransform (item |> getTransform |> Transform2D.setRotation rot)

let getScale (item : CanvasItem) =
    item |> getTransform |> Transform2D.getScale
    
let setScale scale item =
    item |> setTransform (item |> getTransform |> Transform2D.setScale scale)
    
let getSkew item =
    item |> getTransform |> Transform2D.getSkew

let setSkew skew item =
    item |> setTransform (item |> getTransform |> Transform2D.setSkew skew)

let getGlobalTransform (item : CanvasItem) =
    item.GetGlobalTransform()
    
let setGlobalTransform (v : Transform2D)  (item : CanvasItem)=
    item |> GodotObject.set "global_transform" v

let getGlobalPosition (item : CanvasItem) =
    item |> getGlobalTransform |> Transform2D.getOrigin

let setGlobalPosition pos item =
    item |> setGlobalTransform (item |> getGlobalTransform |> Transform2D.setOrigin pos)

let getGlobalRotation (item : CanvasItem) =
    item |> getGlobalTransform |> Transform2D.getRotation

let setGlobalRotation rot item =
    item |> setGlobalTransform (item |> getGlobalTransform |> Transform2D.setRotation rot)

let getGlobalScale (item : CanvasItem) =
    item |> getGlobalTransform |> Transform2D.getScale
    
let setGlobalScale scale item =
    item |> setGlobalTransform (item |> getGlobalTransform |> Transform2D.setScale scale)
    
let getGlobalSkew item =
    item |> getGlobalTransform |> Transform2D.getSkew

let setGlobalSkew skew item =
    item |> setGlobalTransform (item |> getGlobalTransform |> Transform2D.setSkew skew)

type CanvasItem with

    member this.Transform
        with get () = this |> getTransform
        and set v = this |> setTransform v
    
    member this.Position
        with get () = this |> getPosition
        and set v = this |> setPosition v
    
    member this.Rotation
        with get () = this |> getRotation
        and set v = this |> setRotation v
    
    member this.Scale
        with get () = this |> getScale
        and set v = this |> setScale v
    
    member this.Skew
        with get () = this |> getSkew
        and set v = this |> setSkew v
            
    member this.GlobalTransform
        with get() = this |> getGlobalTransform
        and set v = this |> setGlobalTransform v
        
    member this.GlobalPosition
        with get () = this |> getGlobalPosition
        and set v = this |> setGlobalPosition v
    
    member this.GlobalRotation
        with get () = this |> getGlobalRotation
        and set v = this |> setGlobalRotation v
    
    member this.GlobalScale
        with get () = this |> getGlobalScale
        and set v = this |> setGlobalScale v
    
    member this.GlobalSkew
        with get () = this |> getGlobalSkew
        and set v = this |> setGlobalSkew v