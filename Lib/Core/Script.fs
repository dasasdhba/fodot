namespace Lib.Core

open FSharpPlus
open System
open System.Reflection
open System.Collections.Concurrent

[<AttributeUsage(AttributeTargets.Class, AllowMultiple = false)>]
type FScriptAttribute(name: string) =
    inherit Attribute()
    member this.Name = name
    
module Script =
    
    let private cache = ConcurrentDictionary<string, Type option>()
    let private paramCache = ConcurrentDictionary<Type, ConstructorInfo[]>()

    let private initMap () =
        AppDomain.CurrentDomain.GetAssemblies()

        |> Seq.collect (fun asm ->
            try asm.GetTypes()
            with _ -> Array.empty
        )

        |> Seq.choose (fun t ->
            t.GetCustomAttributes(typeof<FScriptAttribute>, false)

            |> Array.tryHead
            |> Option.map (fun attr -> 
                let name = (attr :?> FScriptAttribute).Name
                name, t
            )
        )

        |> Map.ofSeq

    let private typeMap = lazy initMap()

    let private getConstructors (t: Type) =
        paramCache.GetOrAdd(t, fun _ -> t.GetConstructors())

    let createScript (name: string) (args: obj array) : obj option = monad {
        let! typ = 
            cache.GetOrAdd(name, fun key ->
                match typeMap.Value.TryFind key with

                | Some t -> Some t
                | None -> None
            )
        
        let constructors = getConstructors typ

        let! matchedConstructor =
            constructors

            |> Array.tryFind (fun ctor ->
                let parameters = ctor.GetParameters()

                parameters.Length = args.Length &&
                Array.forall2 (fun (param: ParameterInfo) arg ->
                    param.ParameterType.IsAssignableFrom(arg.GetType())
                ) parameters args
            )

        matchedConstructor.Invoke(args)
    }