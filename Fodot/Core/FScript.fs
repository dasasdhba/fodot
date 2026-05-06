namespace Fodot.Core

open System
open System.Collections.Concurrent
open System.Reflection
open FSharpPlus
open Fodot.Common
open Godot
open Fodot.Core.GodotObject
    
module FScript =
    
    let private cache =
        ConcurrentDictionary<string, Result<Type list, string>>()
    let private paramCache =
        ConcurrentDictionary<Type, ConstructorInfo[]>()
    
    let mutable private assemblies : Assembly array = [||]
    
    let setAssemblies (a: Assembly array) =
        assemblies <- a
    
    let private initMap () =
        //AppDomain.CurrentDomain.GetAssemblies()
        assemblies

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
                (*|> Array.tryFind (fun ctor ->
                    let parameters = ctor.GetParameters()

                    parameters.Length = args.Length &&
                    Array.forall2 (fun (param: ParameterInfo) arg ->
                        param.ParameterType.IsAssignableFrom(arg.GetType())
                    ) parameters args
                )*)

            matchedConstructor.Invoke(args)
        })
    }

    type private FScriptData() =
        inherit RefCounted()
        member val Keys = ConcurrentBag<string>() with get
        member val Scripts = ConcurrentBag<Object>() with get

    let private fScriptMeta = "_fs_script_data"

    let private updateScriptData (name : string) (scripts : Object list) (obj : GodotObject) =
        let data = obj |> getMetaWithDefaultAs fScriptMeta (lazy new FScriptData())
        data.Keys.Add name
        scripts |> List.iter (fun s -> data.Scripts.Add s)
        
    let private containsKey (name : string) (obj : GodotObject) =
        let result = monad {
            let! data = obj |> tryGetMetaAs<FScriptData> fScriptMeta
            if data.Keys |> Seq.contains name then
                ()
            else
                return! None
        }
        
        result <> None

    let private getMetaAndGroupList (obj : GodotObject) =
        obj |> getMetaList

        |> List.ofSeq

        |> List.append (
            match obj with
            | :? Node as n -> n.GetGroups () |> List.ofSeq
            | _ -> []
        )

        |> List.choose (fun m ->
            let s = m |> string
            if s.StartsWith "fs_" && s.Length > 3 then
                Some s[3..]
            else
                None
        )
        
    let private getCallbackFScripts (obj : GodotObject) =
        let getCallArrWith (name : string) =
            match obj |> tryInvokeAs<string[]> name with
            
            | Some arr -> arr |> List.ofSeq
            | None -> []
        
        getCallArrWith "_get_fscripts"
        
        |> List.append (getCallArrWith "_GetFScripts")

    let update (obj : GodotObject) =
        let arr =
            obj
            
            |> getMetaAndGroupList
            |> List.append (obj |> getCallbackFScripts)
            |> List.distinct
            |> List.filter (fun s -> obj |> containsKey s |> not)
        
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
            
    let tryGet<'a> (obj : GodotObject) = monad {
        let! data =
            obj |> tryGetMetaAs<FScriptData> fScriptMeta
        
        return!
            data.Scripts
        
            |> Seq.tryFind (fun s -> s :? 'a)
            |> Option.map (fun s -> s :?> 'a)
    }
    
    let get<'a> (obj: GodotObject) =
        obj
        |> tryGet<'a>
        |> Option.defaultWith (fun () -> failwith $"{obj} does not have {typeof<'a>} script.")
        
    let contains<'a> (obj: GodotObject) =
        obj |> tryGet<'a> |> Option.isSome
        
    let attach<'a> (obj : GodotObject) =
        let attr =
            typeof<'a>.GetCustomAttributes(typeof<FScriptAttribute>, false)
            |> Array.tryHead
            |> Option.defaultWith (fun () ->
                failwith $"{typeof<'a>} is not a FScript."
            )
            :?> FScriptAttribute
        let name = attr.Name
        
        if obj |> containsKey name |> not then
            obj |> setMeta $"fs_{name}" true
            obj |> update
        
        obj |> get<'a>