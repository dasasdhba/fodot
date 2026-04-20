module Fodot.Core.Variant

open System
open Godot

let toType<'a> (variant : Variant) =
    match variant.VariantType with
    
    | Variant.Type.Nil -> raise (Exception("Variant: cannot convert a null value."))
    | _ -> variant.As<'a> ()
    
let toSome<'a> (variant : Variant) =
    try
        toType<'a> variant |> Some
    with
    | _ -> None

let from (value: 'a) =
    Variant.From &value