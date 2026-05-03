module Fodot.Core.Callable

open Godot
open System
open Microsoft.FSharp.Reflection

let from<'a, 'b> (f: 'a -> 'b) =
    let action = typeof<'b> = typeof<unit>
    let t = typeof<'a>
    if FSharpType.IsTuple t then
        let tuple = FSharpType.GetTupleElements(t)
        let makeTuple arr = FSharpValue.MakeTuple(arr, t)
        
        match tuple.Length with
        | 2 ->
            if action then
                Callable.From(Action<Variant, Variant>(fun a1 a2 ->
                    f (makeTuple [|a1.Obj; a2.Obj|] :?> 'a) |> ignore))
            else
                Callable.From(Func<Variant, Variant, Variant>(fun a1 a2 ->
                    f (makeTuple [|a1.Obj; a2.Obj|] :?> 'a) |> Variant.from))
        | 3 ->
            if action then
                Callable.From(Action<Variant, Variant, Variant>(fun a1 a2 a3 ->
                    f (makeTuple [|a1.Obj; a2.Obj; a3.Obj|] :?> 'a) |> ignore))
            else
                Callable.From(Func<Variant, Variant, Variant, Variant>(fun a1 a2 a3 ->
                    f (makeTuple [|a1.Obj; a2.Obj; a3.Obj|] :?> 'a) |> Variant.from))
        | 4 ->
            if action then
                Callable.From(Action<Variant, Variant, Variant, Variant>(fun a1 a2 a3 a4 ->
                    f (makeTuple [|a1.Obj; a2.Obj; a3.Obj; a4.Obj|] :?> 'a) |> ignore))
            else
                Callable.From(Func<Variant, Variant, Variant, Variant, Variant>(fun a1 a2 a3 a4 ->
                    f (makeTuple [|a1.Obj; a2.Obj; a3.Obj; a4.Obj|] :?> 'a) |> Variant.from))
        | 5 ->
            if action then
                Callable.From(Action<Variant, Variant, Variant, Variant, Variant>(fun a1 a2 a3 a4 a5 ->
                    f (makeTuple [|a1.Obj; a2.Obj; a3.Obj; a4.Obj; a5.Obj|] :?> 'a) |> ignore))
            else
                Callable.From(Func<Variant, Variant, Variant, Variant, Variant, Variant>(fun a1 a2 a3 a4 a5 ->
                    f (makeTuple [|a1.Obj; a2.Obj; a3.Obj; a4.Obj; a5.Obj|] :?> 'a) |> Variant.from))
        | 6 ->
            if action then
                Callable.From(Action<Variant, Variant, Variant, Variant, Variant, Variant>(fun a1 a2 a3 a4 a5 a6 ->
                    f (makeTuple [|a1.Obj; a2.Obj; a3.Obj; a4.Obj; a5.Obj; a6.Obj|] :?> 'a) |> ignore))
            else
                Callable.From(Func<Variant, Variant, Variant, Variant, Variant, Variant, Variant>(fun a1 a2 a3 a4 a5 a6 ->
                    f (makeTuple [|a1.Obj; a2.Obj; a3.Obj; a4.Obj; a5.Obj; a6.Obj|] :?> 'a) |> Variant.from))
        | 7 ->
            if action then
                Callable.From(Action<Variant, Variant, Variant, Variant, Variant, Variant, Variant>(fun a1 a2 a3 a4 a5 a6 a7 ->
                    f (makeTuple [|a1.Obj; a2.Obj; a3.Obj; a4.Obj; a5.Obj; a6.Obj; a7.Obj|] :?> 'a) |> ignore))
            else
                Callable.From(Func<Variant, Variant, Variant, Variant, Variant, Variant, Variant, Variant>(fun a1 a2 a3 a4 a5 a6 a7 ->
                    f (makeTuple [|a1.Obj; a2.Obj; a3.Obj; a4.Obj; a5.Obj; a6.Obj; a7.Obj|] :?> 'a) |> Variant.from))
        | 8 ->
            if action then
                Callable.From(Action<Variant, Variant, Variant, Variant, Variant, Variant, Variant, Variant>(fun a1 a2 a3 a4 a5 a6 a7 a8 ->
                    f (makeTuple [|a1.Obj; a2.Obj; a3.Obj; a4.Obj; a5.Obj; a6.Obj; a7.Obj; a8.Obj|] :?> 'a) |> ignore))
            else
                Callable.From(Func<Variant, Variant, Variant, Variant, Variant, Variant, Variant, Variant, Variant>(fun a1 a2 a3 a4 a5 a6 a7 a8 ->
                    f (makeTuple [|a1.Obj; a2.Obj; a3.Obj; a4.Obj; a5.Obj; a6.Obj; a7.Obj; a8.Obj|] :?> 'a) |> Variant.from))
        | n ->
            failwith $"Callable.from: arg length {n} is not supported, max length is 8."
    
    elif t = typeof<unit> then
        let arg = () :> obj :?> 'a
        if action then
            Callable.From(Action(fun () -> f arg |> ignore))
        else
            Callable.From(Func<Variant>(fun () -> f arg |> Variant.from))
    
    else
        if action then
            Callable.From(Action<Variant>(fun a -> f (a |> Variant.toType<'a>) |> ignore))
        else
            Callable.From(Func<Variant, Variant>(fun a -> f (a |> Variant.toType<'a>) |> Variant.from))
        
let call<'a> (args: 'a) (callable: Callable) =
    callable.Call(args |> Variant.fromTuple)
    
let callDeferred<'a> (args: 'a) (callable: Callable) =
    callable.CallDeferred(args |> Variant.fromTuple)
    
let invoke (callable: Callable) =
    callable.Call()
    
let invokeDeferred (callable: Callable) =
    callable.CallDeferred()