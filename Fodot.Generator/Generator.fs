module Fodot.Generator.Generator

open System.Collections.Generic
open YamlDotNet.Serialization
open YamlDotNet.Serialization.NamingConventions

type YamlProperty() = 
    [<DefaultValue>]
    val mutable Type: string
    [<DefaultValue>]
    val mutable Value: obj option
    [<DefaultValue>]
    val mutable Hint: string
    [<DefaultValue>]
    val mutable HintString: string
    [<DefaultValue>]
    val mutable Usage: string

type YamlSignalArg() = 
    [<DefaultValue>]
    val mutable Name: string
    [<DefaultValue>]
    val mutable Type: string

type YamlRoot() = 
    [<DefaultValue>]
    val mutable Extends: string
    [<DefaultValue>]
    val mutable Property: obj
    [<DefaultValue>]
    val mutable Signal: obj

let private deserializer =
    let namingConvention = HyphenatedNamingConvention.Instance
    let createObject (typ: System.Type) =
        if typ = typeof<YamlRoot> then YamlRoot() :> obj
        elif typ = typeof<YamlProperty> then YamlProperty() :> obj
        elif typ = typeof<YamlSignalArg> then YamlSignalArg() :> obj
        else System.Activator.CreateInstance(typ)
    DeserializerBuilder()
        .WithNamingConvention(namingConvention)
        .WithObjectFactory(System.Func<System.Type, obj>(createObject))
        .IgnoreUnmatchedProperties()
        .Build()

let parseYaml (content: string) : YamlRoot =
    deserializer.Deserialize<YamlRoot>(content)

let toPascalCase (s: string) =
    let parts = s.Split('_')
    parts 
    |> Array.map (fun part -> 
        if part.Length > 0 && System.Char.IsLower(part.[0]) then
            System.Char.ToUpper(part.[0]).ToString() + part.[1..]
        else part)
    |> String.concat ""

let mapType (t: string) =
    match t with
    | "int" -> "int64"
    | "string" -> "string"
    | x -> x

let generatePropertyCodeNoLet (name: string) (prop: YamlProperty) =
    let fieldName = "_back_prop_" + name
    let gdType = prop.Type
    if gdType.StartsWith("Array[") then
        let innerType = gdType.Replace("Array[", "").Replace("]", "") |> mapType
        "let " + fieldName + " = GDPropArray<" + innerType + ">.From(\"" + name + "\") obj"
    elif gdType.StartsWith("Dictionary[") then
        let innerTypes = gdType.Replace("Dictionary[", "").Replace("]", "")
        let parts = innerTypes.Split([|','|])
        let mapped = parts |> Array.map (fun s -> mapType (s.Trim())) |> String.concat ", "
        "let " + fieldName + " = GDPropDictionary<" + mapped + ">.From(\"" + name + "\") obj"
    else
        let mappedType = mapType gdType
        "let " + fieldName + " = GDProp<" + mappedType + ">.From(\"" + name + "\") obj"

let generateSignalCodeNoLet (name: string) (args: YamlSignalArg list) =
    let fieldName = "_back_signal_" + name
    let argTypes = 
        match args with
        | [] -> "unit"
        | a -> 
            a |> List.map (fun x -> mapType x.Type) 
            |> String.concat " * "
    "let " + fieldName + " = GDSignal<" + argTypes + ">.From(\"" + name + "\") obj"

let generateMemberProperty (name: string) =
    let pascalName = toPascalCase name
    let space8 = "        "
    "member this." + pascalName + "\n" + space8 + "with get () = _back_prop_" + name + ".Get()\n" + space8 + "and set v = _back_prop_" + name + ".Set v"

let generateMemberSignal (name: string) =
    let pascalName = toPascalCase name
    "member val " + pascalName + " = _back_signal_" + name + " with get"

let generateCode (yaml: YamlRoot) (typeName: string) =
    let propsObj = yaml.Property :?> seq<KeyValuePair<obj, obj>>
    let signalsObj = yaml.Signal :?> seq<KeyValuePair<obj, obj>>
    
    let props = Dictionary<string, YamlProperty>()
    let signals = Dictionary<string, YamlSignalArg list>()
    
    if propsObj <> null then
        for kv in propsObj do
            let key = kv.Key.ToString()
            let value: obj = 
                match kv.Value with
                | :? YamlProperty as p -> p :> obj
                | :? string -> 
                    let np = YamlProperty()
                    np.Type <- kv.Value.ToString()
                    np :> obj
                | :? seq<KeyValuePair<obj, obj>> as dict ->
                    let np = YamlProperty()
                    for innerKv in dict do
                        match innerKv.Key.ToString() with
                        | "type" -> np.Type <- innerKv.Value.ToString()
                        | "value" -> np.Value <- Some innerKv.Value
                        | "hint" -> np.Hint <- innerKv.Value.ToString()
                        | "hint_string" -> np.HintString <- innerKv.Value.ToString()
                        | "usage" -> np.Usage <- innerKv.Value.ToString()
                        | _ -> ()
                    np :> obj
                | _ -> null
            if (value :? YamlProperty) then props.Add(key, value :?> YamlProperty) |> ignore
    
    if signalsObj <> null then
        for kv in signalsObj do
            let key = kv.Key.ToString()
            let args = 
                match kv.Value with
                | :? List<YamlSignalArg> as l -> l |> Seq.toList
                | :? IEnumerable<obj> as l ->
                    l |> Seq.choose (fun (x: obj) -> 
                        match x with
                        | :? YamlSignalArg as a -> Some a
                        | :? seq<KeyValuePair<obj, obj>> as dict ->
                            let a = YamlSignalArg()
                            for innerKv in dict do
                                match innerKv.Key.ToString() with
                                | "name" -> a.Name <- innerKv.Value.ToString()
                                | "type" -> a.Type <- innerKv.Value.ToString()
                                | _ -> ()
                            Some a
                        | _ -> None) |> Seq.toList
                | _ -> []
            signals.Add(key, args) |> ignore
    
    let validProps = props |> Seq.filter (fun kv -> not (kv.Key.StartsWith("export_"))) |> Seq.toList
    let prop_fields = validProps |> List.map (fun kv -> generatePropertyCodeNoLet kv.Key kv.Value)
    let signal_fields = signals |> Seq.map (fun kv -> generateSignalCodeNoLet kv.Key kv.Value) |> Seq.toList
    let prop_members = validProps |> List.map (fun kv -> generateMemberProperty kv.Key)
    let signal_members = signals |> Seq.map (fun kv -> generateMemberSignal kv.Key) |> Seq.toList

    let allFields = prop_fields @ signal_fields
    let space4 = "    "
    let fields = String.concat ("\n" + space4) allFields
    let members = String.concat ("\n" + space4) (prop_members @ signal_members @ ["member this.Object = obj"])

    "type " + typeName + "(obj : " + yaml.Extends + ") =\n" +
    space4 + fields + "\n\n" +
    space4 + members