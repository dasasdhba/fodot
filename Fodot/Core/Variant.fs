module Fodot.Core.Variant

open Godot

let toType<'a> (variant : Variant) =
    match variant.VariantType with
    
    | Variant.Type.Nil -> failwith "Variant: cannot convert a null value."
    | _ -> variant.As<'a> ()
    
let toArray<'a> (variant : Variant) =
    variant.AsGodotArray<'a> ()
    
let toDictionary<'a, 'b> (variant : Variant) =
    variant.AsGodotDictionary<'a, 'b> ()
    
let private toSomeTypeWith converter (variant : Variant) =
    try
        converter variant |> Some
    with
    
    | _ -> None
    
let toSome<'a> (variant : Variant) =
    variant |> toSomeTypeWith toType<'a>
    
let toSomeArray<'a> (variant : Variant) =
    variant |> toSomeTypeWith toArray<'a>
    
let toSomeDictionary<'a, 'b> (variant : Variant) =
    variant |> toSomeTypeWith toDictionary<'a, 'b>
    
let from (value: 'a) =
    Variant.From &value