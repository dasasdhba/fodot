module Lib.Moon.Variant

open Godot

let toType<[<MustBeVariant>] 'a> (variant : Variant) =
    variant.As<'a> ()
    
let toSomeType<[<MustBeVariant>] 'a> (variant : Variant) =
    try
        toType<'a> variant |> Some
    with
    | _ -> None

let fromType<[<MustBeVariant>] 'a> (value: 'a) =
    Variant.From &value