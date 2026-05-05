module Fodot.Generator.Parser

open System.Collections.Generic
open System.IO
open YamlDotNet.Serialization

// yaml structure

[<CLIMutable>]
type YamlProperty = {
    
    [<YamlMember(Alias = "type")>]
    Type : string
    
    [<YamlMember(Alias = "value")>]
    Value : string
    
    [<YamlMember(Alias = "hint")>]
    Hint : string
    
    [<YamlMember(Alias = "hint_string")>]
    HintString : string
    
    [<YamlMember(Alias = "usage")>]
    Usage : string
}

[<CLIMutable>]
type YamlSignalArg = {
    
    [<YamlMember(Alias = "name")>]
    Name : string
    
    [<YamlMember(Alias = "type")>]
    Type : string
}

[<CLIMutable>]
type YamlRoot = {
    
    [<YamlMember(Alias = "extends")>]
    Extends : string
    
    [<YamlMember(Alias = "property")>]
    Property : Dictionary<string, YamlProperty>
    
    [<YamlMember(Alias = "signal")>]
    Signal : Dictionary<string, YamlSignalArg list>
}

// process logic

let private mapTypeFs = function
    | "int" -> "int64"
    | "String" -> "string"
    | "Array" -> "Collections.Array"
    | "Dictionary" -> "Collections.Dictionary"
    | s -> s

let private mapTypeGd = function
    | "int64" -> "int"
    | "string" -> "String"
    | s -> s

type PropType =
    | Raw of string
    | TypedArray of string
    | TypedDictionary of string * string
    
    static member From (typ : string) =
        match typ with
        | t when t.StartsWith "Array[" -> 
            let inner = t.Replace("Array[", "").Replace("]", "")
            TypedArray inner
        | t when t.StartsWith "Dictionary[" -> 
            let inner = t.Replace("Dictionary[", "").Replace("]", "")
            let parts = inner.Split([|','|])
            let k = parts[0].Trim()
            let v = parts[1].Trim()
            TypedDictionary (k, v)
        | t -> Raw t
        
    member this.GetTextGd () =
        let mapper = mapTypeGd
        match this with
        | Raw t -> mapper t
        | TypedArray t -> $"Array[{mapper t}]"
        | TypedDictionary (k, v) -> $"Dictionary[{mapper k}, {mapper v}]"
    
    member this.GetTextFs () =
        let mapper = mapTypeFs
        match this with
        | Raw t -> mapper t
        | TypedArray t -> $"Collections.Array<{mapper t}>"
        | TypedDictionary (k, v) -> $"Collections.Dictionary<{mapper k}, {mapper v}>"
    
    member this.GetTextFsType () =
        let mapper = mapTypeFs
        match this with
        | Raw t -> mapper t
        | TypedArray t -> mapper t
        | TypedDictionary (k, v) -> $"{mapper k}, {mapper v}"

type PropertyData = {
    Type : PropType
    Value : string option
    Hint : string option
    HintString : string option
    Usage : string option
}

let private stringAsOption (s: string) =
    match s with
    | null -> None
    | v -> Some v

let private toPascalCase (s: string) =
    s.Split('_')
    
    |> Array.map (fun part -> 
        if part.Length > 0 && System.Char.IsLower(part[0]) then
            System.Char.ToUpper(part[0]).ToString() + part[1..]
        else part)
    
    |> String.concat ""

type ExportProperty =
    | Category
    | Group of string option
    | Subgroup of string option
    | Property of PropertyData
    
    static member From (yaml : YamlProperty) =
        match yaml.Type with
        | "export_category" -> Category
        | "export_group" -> Group (stringAsOption yaml.Value)
        | "export_subgroup" -> Subgroup (stringAsOption yaml.Value)
        | _ -> Property {
            Type = PropType.From yaml.Type
            Value = stringAsOption yaml.Value
            Hint = stringAsOption yaml.Hint
            HintString = stringAsOption yaml.HintString
            Usage = stringAsOption yaml.Usage
        }
        
    member this.AsFsBack name =
        match this with
        | Property p ->
            let pack =
                let fs = p.Type.GetTextFsType()
                match p.Type with
                | Raw _ -> $"GDProp<{fs}>"
                | TypedArray _ -> $"GDPropArray<{fs}>"
                | TypedDictionary _ -> $"GDPropDictionary<{fs}>"
            $"    let _back_prop_{name} = {pack}.From(\"{name}\") obj"
        | _ -> ""
    
    member this.AsFsMember name =
        match this with
        | Property _ ->
            let back = $"_back_prop_{name}"
            let pascal = toPascalCase name
            $"    member this.{pascal}\n        with get () = {back}.Get()\n        and set v = {back}.Set v"
        | _ -> ""
    
    member this.AsGdExport name =
        let exportGroupWith prefix (name: string) (s: string option) =
            match s with
            
            | Some v ->
                $"@export_{prefix}(\"{name}\", \"{v}\")"
            | None ->
                $"@export_{prefix}(\"{name}\")"
        
        match this with
        
        | Category ->
            $"@export_category(\"{name}\")"
        | Group s ->
            exportGroupWith "group" name s
        | Subgroup s ->
            exportGroupWith "subgroup" name s
        | Property p ->
            
            let hint =
                match p.Hint with
                | Some h -> h
                | None -> "PROPERTY_HINT_NONE"
                
            let hintString =
                match p.HintString with
                | Some h -> $"\"{h}\""
                | None -> "\"\""
                
            let export =
                match p.Usage with
                | Some u -> $"@export_custom({hint}, {hintString}, {u})"
                | None -> $"@export_custom({hint}, {hintString})"
                
            let typ = p.Type.GetTextGd()
            
            let value =
                match p.Value with
                | Some v when typ = "String" -> $" = \"{v}\""
                | Some v -> $" = {v}"
                | None -> ""
            
            $"{export} var {name} : {typ}{value}"

let signalToFsBack (name : string) (yaml : YamlSignalArg list) =
    let typ =
        if yaml.IsEmpty then
            "unit"
        else
            yaml
            |> List.map (fun y -> (y.Type |> PropType.From).GetTextFs() )
            |> String.concat ", "
    $"    let _back_signal_{name} = GDSignal<{typ}>.From(\"{name}\") obj"

let signalToFsMember (name : string)=
    let pascal = toPascalCase name
    $"    member val {pascal} = _back_signal_{name} with get"

let signalToGd (name : string) (yaml : YamlSignalArg list) =
    let typ =
        let inner =
            yaml
            
            |> List.map (fun y ->
                let arg = y.Name
                let t = (y.Type |> PropType.From).GetTextGd()
                $"{arg} : {t}"
            )
            
            |> String.concat ", "
        
        if yaml.IsEmpty then
            ""
        else
            $"({inner})"
    
    $"signal {name}{typ}"
    
type YamlRoot with
    member this.AsGd () =
        let extends = $"extends {this.Extends}"
        
        let exports =
            this.Property.Keys
            
            |> List.ofSeq
            |> List.map (fun name ->
                let prop = this.Property[name]
                let prop = ExportProperty.From prop
                prop.AsGdExport name
            )
            |> String.concat "\n"
        
        let signals =
            this.Signal.Keys
            
            |> List.ofSeq
            |> List.map (fun name ->
                let signal = this.Signal[name]
                signalToGd name signal
            )
            |> String.concat "\n"
            
        $"{extends}\n\n{exports}\n\n{signals}"
    
    member this.AsFs fileName =
        let typ = $"type {toPascalCase fileName}(obj : {this.Extends}) ="
        
        let backProp =
            this.Property.Keys
            
            |> List.ofSeq
            |> List.map (fun name ->
                let prop = this.Property[name]
                let prop = ExportProperty.From prop
                prop.AsFsBack name
            )
            |> String.concat "\n"
        
        let backSignal =
            this.Signal.Keys
            
            |> List.ofSeq
            |> List.map (fun name ->
                let signal = this.Signal[name]
                signalToFsBack name signal
            )
            |> String.concat "\n"
        
        let memberProp =
            this.Property.Keys
            
            |> List.ofSeq
            |> List.map (fun name ->
                let prop = this.Property[name]
                let prop = ExportProperty.From prop
                prop.AsFsMember name
            )
            |> String.concat "\n"
            
        let memberSignal =
            this.Signal.Keys
            
            |> List.ofSeq
            |> List.map signalToFsMember
            |> String.concat "\n"
            
        $"{typ}\n{backProp}\n\n{backSignal}\n\n{memberProp}\n\n{memberSignal}"
        
// main builder

let builder =
    DeserializerBuilder()
        .IgnoreUnmatchedProperties()
        .Build()
        
let createGdString (file : string) =
    let content = File.ReadAllText(file)
    let yaml = builder.Deserialize<YamlRoot>(content)
    yaml.AsGd()
    
let createFsString (file : string) =
    let content = File.ReadAllText(file)
    let yaml = builder.Deserialize<YamlRoot>(content)
    let name = Path.GetFileNameWithoutExtension(file) |> toPascalCase
    yaml.AsFs name

let rec findParentFsproj (dir: string) =
    let dir = Path.GetFullPath(dir)
    let files = Directory.GetFiles(dir)
    match files with
    
    | fs when fs |> Array.exists (fun f -> Path.GetExtension(f) = ".sln") ->
        "null"
    | fs ->
        let fsproj = fs |> Array.tryFind (fun f -> Path.GetExtension(f) = ".fsproj")
        match fsproj with
        
        | Some f -> f
        | None ->
            findParentFsproj (Directory.GetParent(dir).FullName)
    
let addCompileItem file fsproj =
    let proj = File.ReadAllText fsproj
    let compile = $"<Compile Include=\"{file}.fs\" />"
    if proj.Contains compile |> not then
        let first = proj.IndexOf "<Compile Include="
        let line = proj.LastIndexOf("\n", first)
        let tab = proj.Substring(line + 1, first - line - 1)
        let newProj = proj.Insert(first, compile + "\n" + tab)
        File.WriteAllText(fsproj, newProj)

let removeCompileItem file fsproj =
    let proj = File.ReadAllText fsproj
    let compile = $"<Compile Include=\"{file}.fs\" />"
    if proj.Contains compile then
        let first = proj.IndexOf compile
        let line = proj.LastIndexOf ("\n", first)
        let newProj = proj.Remove(line, first - line + compile.Length)
        File.WriteAllText(fsproj, newProj)

// cli
    
let rec getYamlFiles (dir: string) : Dictionary<string, string list>  =
    let dir = Path.GetFullPath(dir)
    let files = Directory.GetFiles(dir, "*.yaml")
    let dict = Dictionary<string, string list>()
    if files.Length > 0 then
        let fsproj = findParentFsproj dir
        dict[fsproj] <- files |> List.ofArray
    
    Directory.GetDirectories(dir)
    
    |> Array.fold (fun acc d ->
        let m = getYamlFiles d
        for k in m.Keys do
            if acc.ContainsKey(k) then
                acc[k] <- acc[k] @ m[k]
            else
                acc[k] <- m[k]
        acc
    ) dict
    
let createFsBinding (inputDir : string)=
    if not (Directory.Exists(inputDir)) then
        printfn $"Input directory does not exist: {inputDir}"
    else
        let inputDir = Path.GetFullPath(inputDir)
        let yamlFiles = getYamlFiles inputDir
        
        for k in yamlFiles.Keys do
            let files = yamlFiles[k]
            match k with
            | "null" ->
                let all =
                    files
                    |> List.map Path.GetFileNameWithoutExtension
                    |> String.concat ", "
                printfn $"Cannot find parent fsproj for {all}.\nBinding will not be created."
            | fsproj ->
                let codes = 
                    files 
                    |> List.map createFsString

                let name = Path.GetFileNameWithoutExtension(fsproj)
                let fullCode = 
                    $"namespace {name}.Bind\n\n" +
                    "open Fodot.Core\n" +
                    "open Godot\n\n" +
                    (codes |> String.concat "\n\n")
                
                let file = Path.GetDirectoryName(fsproj) + "/Bind.fs"
                File.WriteAllText(file, fullCode)
                addCompileItem "Bind" fsproj
                printfn $"Generated {files.Length} binding types for {name}"