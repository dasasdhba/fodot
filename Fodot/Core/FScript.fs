namespace Fodot.Core

open System
open System.Collections.Concurrent
open System.Reflection
open FSharpPlus
open Fodot
open Godot
open Fodot.Core.GodotObject

[<AttributeUsage(AttributeTargets.Class, AllowMultiple = false)>]
type FScriptAttribute(name: string) =
    inherit Attribute()
    member this.Name = name
    
module FScript =
    
    let private cache =
        ConcurrentDictionary<string, Result<Type list, string>>()
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
        
        |> Seq.fold (fun (s: ConcurrentDictionary<string, Type list>) (name, t) ->
            if s.ContainsKey name then
                s[name] <- t :: s[name]
            else
                s[name] <- [t]
            s
        ) (ConcurrentDictionary<string, Type list>())

    let private typeMap = lazy initMap()

    let private getConstructors (t: Type) =
        paramCache.GetOrAdd(t, fun _ -> t.GetConstructors())
        
    let buildCache () =
        for k in typeMap.Value.Keys do
            let ts = typeMap.Value[k]
            cache[k] <- Ok ts
            ts |> List.iter (fun t -> getConstructors t |> ignore)

    let private create (name: string) (args: obj array) = monad {
        let! typs = 
            cache.GetOrAdd(name, fun key ->
                let has, result = typeMap.Value.TryGetValue key
                if has |> not then
                    Result.Error $"the script {name} was not found in F# library"
                else
                    Ok result
            )
        
        typs
        
        |> List.choose (fun typ -> monad {
            let constructors = getConstructors typ

            let! matchedConstructor =
                constructors
                |> Array.tryHead

                // we don't really need multiple constructors
                //|> Array.tryFind (fun ctor ->
                //    let parameters = ctor.GetParameters()

                //    parameters.Length = args.Length &&
                //    Array.forall2 (fun (param: ParameterInfo) arg ->
                //        param.ParameterType.IsAssignableFrom(arg.GetType())
                //    ) parameters args
                //)

            matchedConstructor.Invoke(args)
        })
    }

    type private FScriptData() =
        inherit Resource()
        member val Keys = ResizeArray<string>() with get
        member val Scripts = ResizeArray<Object>() with get

    let private fScriptMeta = "_fs_script_data"

    let private updateScriptData (name : string) (scripts : Object list) (obj : GodotObject) =
        let data = obj |> getMetaWithDefault fScriptMeta (lazy new FScriptData())
        data.Keys.Add name
        data.Scripts.AddRange scripts
        
    let private containsKey (name : string) (obj : GodotObject) =
        let result = monad {
            let! data = obj |> getSomeMeta<FScriptData> fScriptMeta
            if data.Keys.Contains name then
                ()
            else
                return! None
        }
        
        result <> None

    let private getMetaAndGroupListWith filter (obj : GodotObject) =
        obj |> getMetaList

        |> List.ofSeq

        |> List.append (
            match obj with
            | :? Node as n -> n.GetGroups () |> List.ofSeq
            | _ -> []
        )

        |> List.choose (fun m ->
            let s = m |> string
            if s |> filter then
                Some s
            else
                None
        )
        
    let private getCallbackFScripts (obj : GodotObject) =
        let getCallArrWith (name : string) =
            match obj |> callSome<string[]> name [||] with
            
            | Some arr -> arr |> List.ofSeq
            | None -> []
        
        getCallArrWith "_get_fscripts"
        
        |> List.append (getCallArrWith "_GetFScripts")

    let update (obj : GodotObject) =
        let arr =
            obj
            
            |> getMetaAndGroupListWith (fun s -> s.StartsWith "fs_" && s.Length > 3)
            |> List.map (fun s -> s[3..])
            |> List.append (obj |> getCallbackFScripts)
            |> List.filter (fun s -> obj |> containsKey s |> not)
            |> List.distinct
        
        for m in arr do
            try
                match create m [|obj|] with
                
                | Ok scripts ->
                    obj |> updateScriptData m scripts
                | Error e ->
                    raise (Exception e)
            with
            
            | ex -> Logger.pushError $"{obj}: failed creating script {m}: {ex}"
            
    let init (obj : GodotObject) =
        if obj |> hasMeta fScriptMeta then
            ()
        else
            obj |> update
            
    let get<'a> (obj : GodotObject) = monad {
        let! data =
            obj
            
            |> getSomeMeta<FScriptData> fScriptMeta
            |> Option.toResultWith $"{obj}: the fsharp script has not been initialized yet"
        
        return!
            data.Scripts
        
            |> Seq.tryFind (fun s -> s :? 'a)
            |> Option.map (fun s -> s :?> 'a)
            |> Option.toResultWith $"{obj}: the script {typeof<'a>} was not found"
    }
    
    let getSome<'a> (obj: GodotObject) =
        obj |> get<'a> |> Option.ofResult
        
    let contains<'a> (obj: GodotObject) =
        obj |> getSome<'a> |> Option.isSome