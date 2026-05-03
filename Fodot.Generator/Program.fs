open System.IO
open Fodot.Generator.Generator

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
        let inputDir = args.[0]
        let outputFile = args.[1]
        
        if not (Directory.Exists(inputDir)) then
            printfn "Input directory does not exist: %s" inputDir
            1
        else
            let yamlFiles = getYamlFiles inputDir
            printfn "Found %d yaml files" yamlFiles.Length
            
            let codes = 
                yamlFiles 
                |> Array.map (fun file ->
                    let content = File.ReadAllText(file)
                    let yaml = parseYaml content
                    let fileName = Path.GetFileNameWithoutExtension(file)
                    let typeName = toPascalCase fileName
                    generateCode yaml typeName)
                |> Array.toList
            
            let fullCode = 
                "namespace Fodot.Bind\n\n" +
                "open Fodot.Core\n" +
                "open Godot\n\n" +
                (codes |> String.concat "\n")
            
            File.WriteAllText(outputFile, fullCode)
            printfn "Generated: %s" outputFile
            printfn "Done!"
            0