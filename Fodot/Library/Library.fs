namespace Fodot.Library

open Fodot.Core
open Godot

type GDLib(path : string) =
    let res = GD.load path
    let dict =
        res |> GodotObject.getAsDictionary<string, Resource> "lib"
    
    member this.Get<'a when 'a :> Resource> (key : string) =
        dict[key] :?> 'a
    member this.Lib = dict