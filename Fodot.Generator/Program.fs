open System.IO
open Fodot.Generator.Parser

let rec getYamlFiles (dir: string) =
    let files = Directory.GetFiles(dir, "*.yaml")
    let subDirs = Directory.GetDirectories(dir)
    let subFiles = subDirs |> Array.collect getYamlFiles
    Array.concat [files; subFiles]

[<EntryPoint>]
let main args =
    if args.Length < 2 then
        printfn "Usage: Fodot.Generator <inputDir> <outputFile>"
        1
    else
        let inputDir = args[0]
        let outputFile = args[1]
        
        if not (Directory.Exists(inputDir)) then
            printfn $"Input directory does not exist: {inputDir}"
            1
        else
            let outputDir = Path.GetDirectoryName(outputFile)
            if outputDir <> "" && not (Directory.Exists(outputDir)) then
                Directory.CreateDirectory(outputDir) |> ignore
            
            let yamlFiles = getYamlFiles inputDir
            printfn $"Found {yamlFiles.Length} yaml files"
            
            let codes = 
                yamlFiles 
                |> Array.map (fun file -> createFsString file)
                |> Array.toList
            
            let fullCode = 
                "namespace Fodot.Bind\n\n" +
                "open Fodot.Core\n" +
                "open Godot\n\n" +
                (codes |> String.concat "\n\n")
            
            File.WriteAllText(outputFile, fullCode)
            printfn $"Generated: %s{outputFile}"
            printfn "Done!"
            0