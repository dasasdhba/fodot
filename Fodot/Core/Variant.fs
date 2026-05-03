module Fodot.Core.Variant

open Godot
open System
open Microsoft.FSharp.Reflection

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

let fromObj (value: obj) =
    match value with
    | null -> failwith "Variant: cannot convert a null value."
    | :? Variant as v -> v
    | :? bool as v -> Variant.CreateFrom v
    | :? char as v -> Variant.CreateFrom v
    | :? sbyte as v -> Variant.CreateFrom v
    | :? int16 as v -> Variant.CreateFrom v
    | :? int as v -> Variant.CreateFrom v
    | :? Enum as v -> Variant.CreateFrom (Convert.ToInt32(v))
    | :? int64 as v -> Variant.CreateFrom v
    | :? byte as v -> Variant.CreateFrom v
    | :? uint16 as v -> Variant.CreateFrom v
    | :? uint as v -> Variant.CreateFrom v
    | :? uint64 as v -> Variant.CreateFrom v
    | :? float32 as v -> Variant.CreateFrom v
    | :? float as v -> Variant.CreateFrom v
    | :? string as v -> Variant.CreateFrom v
    | :? Vector2 as v -> Variant.CreateFrom v
    | :? Vector2I as v -> Variant.CreateFrom v
    | :? Rect2 as v -> Variant.CreateFrom v
    | :? Rect2I as v -> Variant.CreateFrom v
    | :? Transform2D as v -> Variant.CreateFrom v
    | :? Vector3 as v -> Variant.CreateFrom v
    | :? Vector3I as v -> Variant.CreateFrom v
    | :? Basis as v -> Variant.CreateFrom v
    | :? Quaternion as v -> Variant.CreateFrom v
    | :? Transform3D as v -> Variant.CreateFrom v
    | :? Vector4 as v -> Variant.CreateFrom v
    | :? Vector4I as v -> Variant.CreateFrom v
    | :? Projection as v -> Variant.CreateFrom v
    | :? Aabb as v -> Variant.CreateFrom v
    | :? Color as v -> Variant.CreateFrom v
    | :? Plane as v -> Variant.CreateFrom v
    | :? Callable as v -> Variant.CreateFrom v
    | :? Signal as v -> Variant.CreateFrom v
    | :? Collections.Array as v -> Variant.CreateFrom v
    | :? Collections.Dictionary as v -> Variant.CreateFrom v
    | :? GodotObject as v -> Variant.CreateFrom v
    | :? StringName as v -> Variant.CreateFrom v
    | :? NodePath as v -> Variant.CreateFrom v
    | :? Rid as v -> Variant.CreateFrom v
    | _ -> failwith $"Variant: cannot convert {value}."

let fromTuple (value: 'a) =
    match typeof<'a> with
    | t when t |> FSharpType.IsTuple ->
        FSharpValue.GetTupleFields value
        |> Array.map fromObj
    | t when t = typeof<unit> -> [||]
    | _ -> [|from value|]