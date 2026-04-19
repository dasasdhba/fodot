namespace Lib.Core

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Reflection
open FSharpPlus
open Godot
open Lib.Core.GodotObject

[<AttributeUsage(AttributeTargets.Class, AllowMultiple = false)>]
type FScriptAttribute(name: string) =
    inherit Attribute()
    member this.Name = name
    
module FScript =
    
    let private cache =
        ConcurrentDictionary<string, Result<Type, string>>()
    let private paramCache =
        ConcurrentDictionary<Type, ConstructorInfo[]>()

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

    let create (name: string) (args: obj array) = monad {
        let! typ = 
            cache.GetOrAdd(name, fun key ->
                typeMap.Value.TryFind key
                |> Option.toResultWith $"the script {name} was not found in F# library"
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
            
            |> Option.toResultWith $"the script {name} does not have a constructor with the specified arguments {args}"

        matchedConstructor.Invoke(args)
    }

    type private FScriptData() =
        inherit Resource()
        member val Scripts = Dictionary<string, Object>()

    let private fScriptMeta = "_FScriptData"

    let private updateScriptData (name : string) (script : Object) (obj : GodotObject) =
        let data = obj |> getMetaWithDefault fScriptMeta (lazy new FScriptData())
        let dict = data.Scripts
        dict[name] <- script
        
    let contains (name : string) (obj : GodotObject) =
        let result = monad {
            let! data = obj |> getSomeMeta<FScriptData> fScriptMeta
            let dict = data.Scripts
            if dict.ContainsKey name then
                ()
            else
                return! None
        }
        
        result <> None

    let init (obj : GodotObject) =
        let arr =
            obj
            
            |> getMetaAndGroupListWith (fun s -> s.StartsWith "fs_" && s.Length > 3)
            |> List.map (fun s -> s[3..])
            |> List.filter (fun s -> obj |> contains s |> not)
        
        for m in arr do
            try
                match create m [|obj|] with
                
                | Ok script ->
                    obj |> updateScriptData m script
                | Error e ->
                    raise (Exception e)
            with
            
            | ex -> GD.PushError $"{obj}: failed to create script {m}: {ex}"
            
    let get<'a> (obj : GodotObject) = monad {
        let! data =
            obj
            
            |> getSomeMeta<FScriptData> fScriptMeta
            |> Option.toResultWith $"{obj}: the fsharp script has not been initialized yet"
        
        return!
            data.Scripts.Values
        
            |> Seq.tryFind (fun s -> s :? 'a)
            |> Option.map (fun s -> s :?> 'a)
            |> Option.toResultWith $"{obj}: the script {typeof<'a>} was not found"
    }