namespace Fodot.Core

open Godot

type GDLib(path : string) =
    let res = GD.load path
    let dict =
        res |> GodotObject.getAsDictionary<string, Variant> "lib"
    
    member this.Get<'a> (key : string) =
        dict[key] |> Variant.toType<'a>
    member this.Lib = dict